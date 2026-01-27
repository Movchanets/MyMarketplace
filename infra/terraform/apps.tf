resource "azurerm_container_app" "api" {
  name                         = var.api_name
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  registry {
    server               = "ghcr.io"
    username             = var.github_username
    password_secret_name = "github-token"
  }

  secret {
    name  = "github-token"
    value = var.github_token
  }

  ingress {
    external_enabled = true
    target_port      = 80
    transport        = "auto"
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0
    max_replicas = 1

    http_scale_rule {
      name                = "http-rule"
      concurrent_requests = "10"
    }

    container {
      name   = "api"
      image  = var.api_image
      cpu    = 0.25
      memory = "0.5Gi"
      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "Storage__Provider"
        value = "azure"
      }
      env {
        name  = "ConnectionStrings__NeonConnection"
        value = var.database_connection_string
      }
      env {
        name  = "Storage__Azure__ConnectionString"
        value = azurerm_storage_account.static.primary_connection_string
      }
      env {
        name  = "Storage__Azure__ContainerName"
        value = var.storage_images_container_name
      }
      env {
        name  = "Storage__Azure__CdnUrl"
        value = var.azure_cdn_url
      }
      env {
        name  = "JwtSettings__AccessTokenSecret"
        value = var.jwt_access_token_secret
      }
      env {
        name  = "JwtSettings__RefreshTokenSecret"
        value = var.jwt_refresh_token_secret
      }
      env {
        name  = "SmtpSettings__Username"
        value = var.smtp_username
      }
      env {
        name  = "SmtpSettings__Password"
        value = var.smtp_password
      }
      env {
        name  = "GoogleAuth__ClientId"
        value = var.google_client_id
      }
      env {
        name  = "GoogleAuth__ClientSecret"
        value = var.google_client_secret
      }
      env {
        name  = "Turnstile__Secret"
        value = var.turnstile_secret
      }
      env {
        name  = "AllowedCorsOrigins"
        value = var.allowed_cors_origins != "" ? var.allowed_cors_origins : azurerm_storage_account.static.primary_web_endpoint
      }
      env {
        name  = "Redis__Enabled"
        value = tostring(var.redis_enabled)
      }
      env {
        name  = "Redis__ConnectionString"
        value = var.redis_connection_string
      }
      env {
        name  = "Redis__InstanceName"
        value = var.redis_instance_name
      }
    }
  }

}

# Front container app disabled - using Azure Storage Static Website instead
# Static website is more cost-effective and CDN-ready for React SPA
