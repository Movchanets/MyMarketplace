namespace Domain.Constants;

/// <summary>
/// Centralized list of all available permissions in the system.
/// Use these constants when creating policies and assigning permissions to roles.
/// </summary>
public static class Permissions
{
    // Users management
    public const string UsersManage = "users.manage";
    public const string UsersRead = "users.read";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    // Roles management
    public const string RolesManage = "roles.manage";
    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";

    // Stores management
    public const string StoresManage = "stores.manage";
    public const string StoresVerify = "stores.verify";
    public const string StoresReadAll = "stores.read.all";
    public const string StoresSuspend = "stores.suspend";
    public const string StoresDelete = "stores.delete";
    public const string StoreUpdateSelf = "store.update.self";
    public const string StoreReadSelf = "store.read.self";

    // Products management
    public const string ProductsCreate = "products.create";
    public const string ProductsReadSelf = "products.read.self";
    public const string ProductsUpdateSelf = "products.update.self";
    public const string ProductsDeleteSelf = "products.delete.self";

    // Categories & Tags
    public const string CategoriesManage = "categories.manage";
    public const string TagsManage = "tags.manage";

    // Orders
    public const string OrdersCreate = "orders.create";
    public const string OrdersReadSelf = "orders.read.self";
    public const string OrdersUpdateStatus = "orders.update.status";

    // Reviews
    public const string ReviewsCreate = "reviews.create";
    public const string ReviewsUpdateSelf = "reviews.update.self";

    // Profile
    public const string ProfileReadSelf = "profile.read.self";
    public const string ProfileUpdateSelf = "profile.update.self";

    // Payouts
    public const string PayoutsReadAll = "payouts.read.all";
    public const string PayoutsProcess = "payouts.process";
    public const string PayoutsReadSelf = "payouts.read.self";
    public const string PayoutsRequest = "payouts.request";

    /// <summary>
    /// Get all available permissions grouped by category
    /// </summary>
    public static Dictionary<string, List<PermissionInfo>> GetAllGrouped()
    {
        return new Dictionary<string, List<PermissionInfo>>
        {
            ["Users"] = new()
            {
                new(UsersManage, "Full user management access"),
                new(UsersRead, "View users list"),
                new(UsersUpdate, "Update user data"),
                new(UsersDelete, "Delete users")
            },
            ["Roles"] = new()
            {
                new(RolesManage, "Full roles management access"),
                new(RolesRead, "View roles list"),
                new(RolesCreate, "Create new roles"),
                new(RolesUpdate, "Update existing roles"),
                new(RolesDelete, "Delete roles")
            },
            ["Stores"] = new()
            {
                new(StoresManage, "Full stores management"),
                new(StoresVerify, "Verify store applications"),
                new(StoresReadAll, "View all stores"),
                new(StoresSuspend, "Suspend stores"),
                new(StoresDelete, "Delete stores"),
                new(StoreUpdateSelf, "Update own store"),
                new(StoreReadSelf, "Read own store")
            },
            ["Products"] = new()
            {
                new(ProductsCreate, "Create products"),
                new(ProductsReadSelf, "View own products"),
                new(ProductsUpdateSelf, "Update own products"),
                new(ProductsDeleteSelf, "Delete own products")
            },
            ["Categories & Tags"] = new()
            {
                new(CategoriesManage, "Manage categories"),
                new(TagsManage, "Manage tags")
            },
            ["Orders"] = new()
            {
                new(OrdersCreate, "Create orders"),
                new(OrdersReadSelf, "View own orders"),
                new(OrdersUpdateStatus, "Update order status")
            },
            ["Reviews"] = new()
            {
                new(ReviewsCreate, "Create reviews"),
                new(ReviewsUpdateSelf, "Update own reviews")
            },
            ["Profile"] = new()
            {
                new(ProfileReadSelf, "View own profile"),
                new(ProfileUpdateSelf, "Update own profile")
            },
            ["Payouts"] = new()
            {
                new(PayoutsReadAll, "View all payouts"),
                new(PayoutsProcess, "Process payouts"),
                new(PayoutsReadSelf, "View own payouts"),
                new(PayoutsRequest, "Request payout")
            }
        };
    }

    /// <summary>
    /// Get flat list of all permissions
    /// </summary>
    public static List<string> GetAll()
    {
        return
        [
            UsersManage, UsersRead, UsersUpdate, UsersDelete,
            RolesManage, RolesRead, RolesCreate, RolesUpdate, RolesDelete,
            StoresManage, StoresVerify, StoresReadAll, StoresSuspend, StoresDelete, StoreUpdateSelf, StoreReadSelf,
            ProductsCreate, ProductsReadSelf, ProductsUpdateSelf, ProductsDeleteSelf,
            CategoriesManage, TagsManage,
            OrdersCreate, OrdersReadSelf, OrdersUpdateStatus,
            ReviewsCreate, ReviewsUpdateSelf,
            ProfileReadSelf, ProfileUpdateSelf,
            PayoutsReadAll, PayoutsProcess, PayoutsReadSelf, PayoutsRequest
        ];
    }
}

/// <summary>
/// Permission info record for grouping
/// </summary>
public record PermissionInfo(string Name, string Description);
