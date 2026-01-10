variable "location" {
  type    = string
  default = "westeurope"
}

variable "resource_group_name" {
  type    = string
  default = "rg-mymarketplace"
}

variable "env_name" {
  type    = string
  default = "cae-mymarketplace"
}

variable "api_name" {
  type    = string
  default = "mymarketplace-api"
}

variable "front_name" {
  type    = string
  default = "mymarketplace-front"
}

variable "storage_account_name" {
  type    = string
  default = "stmymarketplaceweb"
}

variable "storage_container_name" {
  type        = string
  description = "Blob container name to create for API assets (e.g., images)"
  default     = "images"
}

variable "enable_front_container_app" {
  type        = bool
  description = "If true, create a Front Container App (use Storage Static Website instead to minimize cost)."
  default     = false
}

variable "api_image" {
  type        = string
  description = "API container image (e.g., ghcr.io/owner/repo/app-api:latest). Defaults to a placeholder hello-world image."
  default     = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
}

variable "github_username" {
  type        = string
  description = "GitHub username for ghcr.io authentication (e.g., github.actor)"
  default     = ""
}

variable "github_token" {
  type        = string
  description = "GitHub Personal Access Token for ghcr.io authentication"
  sensitive   = true
  default     = ""
}

variable "database_connection_string" {
  type        = string
  description = "Neon PostgreSQL connection string"
  sensitive   = true
}

variable "storage_images_container_name" {
  type        = string
  description = "Azure Blob Storage container name for image uploads"
  default     = "images"
}

variable "azure_cdn_url" {
  type        = string
  description = "Optional CDN URL for blob storage"
  default     = ""
}

variable "jwt_access_token_secret" {
  type        = string
  description = "JWT access token secret key"
  sensitive   = true
}

variable "jwt_refresh_token_secret" {
  type        = string
  description = "JWT refresh token secret key"
  sensitive   = true
}

variable "smtp_username" {
  type        = string
  description = "SMTP username for email service"
  sensitive   = true
}

variable "smtp_password" {
  type        = string
  description = "SMTP password for email service"
  sensitive   = true
}

variable "turnstile_secret" {
  type        = string
  description = "Cloudflare Turnstile secret key"
  sensitive   = true
}

variable "allowed_cors_origins" {
  type        = string
  description = "Allowed CORS origins (comma-separated). Defaults to static website URL if empty."
  default     = ""
}

variable "redis_enabled" {
  type        = bool
  description = "Enable Redis caching"
  default     = false
}

variable "redis_connection_string" {
  type        = string
  description = "Redis connection string (e.g., your-redis.redis.cache.windows.net:6380,password=xxx,ssl=True)"
  sensitive   = true
  default     = ""
}

variable "redis_instance_name" {
  type        = string
  description = "Redis instance name prefix"
  default     = "MyMarketplace:"
}
