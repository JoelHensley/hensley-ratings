variable "region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "instance_type" {
  description = "EC2 instance type"
  type        = string
  default     = "t3.small"
}

variable "domain_name" {
  description = "Primary domain name"
  type        = string
  default     = "hensleyratings.com"
}

variable "public_key_path" {
  description = "Path to SSH public key to import as EC2 key pair"
  type        = string
  default     = "~/.ssh/hensley-ratings.pub"
}
