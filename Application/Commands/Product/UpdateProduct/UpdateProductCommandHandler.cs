using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Product.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ServiceResponse>
{
   private readonly IProductRepository _productRepository;
   private readonly IStoreRepository _storeRepository;
   private readonly ICategoryRepository _categoryRepository;
   private readonly ITagRepository _tagRepository;
   private readonly IProductGalleryRepository _galleryRepository;
   private readonly IUnitOfWork _unitOfWork;
   private readonly ILogger<UpdateProductCommandHandler> _logger;
   private readonly IUserRepository _userRepository;

   public UpdateProductCommandHandler(
      IProductRepository productRepository,
      IStoreRepository storeRepository,
      ICategoryRepository categoryRepository,
      ITagRepository tagRepository,
      IProductGalleryRepository galleryRepository,
      IUnitOfWork unitOfWork,
      ILogger<UpdateProductCommandHandler> logger,
      IUserRepository userRepository)
   {
      _productRepository = productRepository;
      _storeRepository = storeRepository;
      _categoryRepository = categoryRepository;
      _tagRepository = tagRepository;
      _galleryRepository = galleryRepository;
      _unitOfWork = unitOfWork;
      _logger = logger;
      _userRepository = userRepository;
   }

   public async Task<ServiceResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
   {
      _logger.LogInformation("Updating product {ProductId} for user {UserId}", request.ProductId, request.UserId);

      try
      {
         // 1. Validations
         var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
         if (domainUser == null)
         {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return new ServiceResponse(false, "User not found");
         }

         var store = await _storeRepository.GetByUserIdAsync(domainUser.Id);
         if (store == null)
         {
            _logger.LogWarning("Store for user {UserId} not found", request.UserId);
            return new ServiceResponse(false, "Store not found");
         }

         var product = await _productRepository.GetByIdAsync(request.ProductId);

         if (product == null || product.StoreId != store.Id)
         {
            _logger.LogWarning("Product {ProductId} not found for store {StoreId}", request.ProductId, store.Id);
            return new ServiceResponse(false, "Product not found");
         }

         // === UPDATE BASIC INFO ===
         product.Rename(request.Name);
         product.UpdateDescription(request.Description);

         // === UPDATE CATEGORIES ===
         var requestCategoryIds = (request.CategoryIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

         var currentCategories = product.ProductCategories.ToList();
         var currentCategoryIds = currentCategories.Select(pc => pc.CategoryId).ToHashSet();

         // 1. Видалити категорії яких немає в запиті (через репозиторій)
         var toRemoveIds = currentCategoryIds.Except(requestCategoryIds).ToList();
         foreach (var categoryId in toRemoveIds)
         {
            var pcToRemove = currentCategories.First(pc => pc.CategoryId == categoryId);
            _productRepository.RemoveProductCategory(pcToRemove);
         }

         // 2. Додати нові категорії яких немає в продукті (через репозиторій)
         var toAddIds = requestCategoryIds.Except(currentCategoryIds).ToList();
         foreach (var categoryId in toAddIds)
         {
            if (!await _categoryRepository.ExistsAsync(categoryId))
            {
               return new ServiceResponse(false, $"Category {categoryId} not found");
            }

            // Визначаємо чи це буде primary (якщо це єдина категорія)
            var isPrimary = currentCategories.Count == 0 && toAddIds.IndexOf(categoryId) == 0 && toRemoveIds.Count == currentCategoryIds.Count;
            var newPc = ProductCategory.CreateById(product.Id, categoryId, isPrimary);
            _productRepository.AddProductCategory(newPc);
         }

         // 3. Встановити primary category (якщо вказано)
         if (request.PrimaryCategoryId.HasValue && request.PrimaryCategoryId != Guid.Empty)
         {
            if (!requestCategoryIds.Contains(request.PrimaryCategoryId.Value))
            {
               return new ServiceResponse(false, "Primary category must be one of the selected categories");
            }
            product.SetPrimaryCategory(request.PrimaryCategoryId.Value);
         }

         // === UPDATE TAGS ===
         var requestTagIds = (request.TagIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

         var currentTags = product.ProductTags.ToList();
         var currentTagIds = currentTags.Select(pt => pt.TagId).ToHashSet();

         // 1. Видалити теги яких немає в запиті (через репозиторій)
         var tagsToRemoveIds = currentTagIds.Except(requestTagIds).ToList();
         foreach (var tagId in tagsToRemoveIds)
         {
            var ptToRemove = currentTags.First(pt => pt.TagId == tagId);
            _productRepository.RemoveProductTag(ptToRemove);
         }

         // 2. Додати нові теги яких немає в продукті (через репозиторій)
         var tagsToAddIds = requestTagIds.Except(currentTagIds).ToList();
         foreach (var tagId in tagsToAddIds)
         {
            if (!await _tagRepository.ExistsAsync(tagId))
            {
               return new ServiceResponse(false, $"Tag {tagId} not found");
            }

            var newPt = ProductTag.CreateById(product.Id, tagId);
            _productRepository.AddProductTag(newPt);
         }

         // === UPDATE GALLERY (DELETE) ===
         if (request.GalleryIdsToDelete is { Count: > 0 })
         {
            foreach (var galleryId in request.GalleryIdsToDelete)
            {
               var galleryItem = product.Gallery.FirstOrDefault(g => g.Id == galleryId);
               if (galleryItem != null)
               {
                  await _galleryRepository.DeleteWithFileAsync(galleryItem);
                  product.RemoveGalleryItem(galleryId);

                  _logger.LogInformation("Removed gallery item {GalleryId} from product {ProductId}",
                     galleryId, product.Id);
               }
            }
         }

         // === UPDATE GALLERY (ADD NEW IMAGES) ===
         if (request.NewGalleryImages is { Count: > 0 })
         {
            var currentMaxOrder = product.Gallery.Any()
               ? product.Gallery.Max(g => g.DisplayOrder)
               : -1;

            foreach (var imageUpload in request.NewGalleryImages)
            {
               currentMaxOrder++;

               var uploadRequest = new GalleryImageUploadRequest(
                  imageUpload.FileStream,
                  imageUpload.FileName,
                  imageUpload.ContentType,
                  currentMaxOrder
               );

               var result = await _galleryRepository.UploadAndAddAsync(product, uploadRequest);

               // Set as base image if product doesn't have one
               if (string.IsNullOrEmpty(product.BaseImageUrl))
               {
                  product.UpdateBaseImage(result.PublicUrl);
               }

               _logger.LogInformation("Added gallery image {MediaImageId} to product {ProductId}",
                  result.MediaImageId, product.Id);
            }
         }

         // === SAVE ALL CHANGES AT ONCE ===
         await _unitOfWork.SaveChangesAsync(cancellationToken);

         _logger.LogInformation("Product {ProductId} updated successfully", product.Id);
         return new ServiceResponse(true, "Product updated successfully");
      }
      catch (DbUpdateConcurrencyException ex)
      {
         // Логування деталей конфлікту
         var entryInfo = ex.Entries.Count == 0
           ? "<none>"
           : string.Join(", ", ex.Entries.Select(e =>
           {
              var pk = e.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
              var keyStr = pk != null ? $"{pk.Metadata.Name}={pk.CurrentValue}" : "<no-pk>";
              return $"{e.Metadata.ClrType.Name}({keyStr}) - State: {e.State}";
           }));

         _logger.LogError(ex, "Concurrency error. Details: {Entries}", entryInfo);
         return new ServiceResponse(false, $"Concurrency error. Entity: {entryInfo}");
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Error updating product {ProductId}", request.ProductId);
         return new ServiceResponse(false, $"Error: {ex.GetType().Name}: {ex.Message}");
      }
   }
}