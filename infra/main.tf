terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  backend "s3" {
    bucket         = "hensley-ratings-tfstate"
    key            = "prod/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "hensley-ratings-tflock"
    encrypt        = true
    profile        = "terraform-deploy"
  }
}

provider "aws" {
  region  = var.region
  profile = "terraform-deploy"
}

resource "aws_key_pair" "hensley_ratings" {
  key_name   = "hensley-ratings"
  public_key = file(var.public_key_path)
}

resource "aws_security_group" "hensley_ratings" {
  name        = "hensley-ratings-sg"
  description = "Allow SSH, HTTP, and HTTPS"

  ingress {
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

data "aws_ami" "amazon_linux_2023" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }
}

resource "aws_instance" "hensley_ratings" {
  ami                    = data.aws_ami.amazon_linux_2023.id
  instance_type          = var.instance_type
  key_name               = aws_key_pair.hensley_ratings.key_name
  vpc_security_group_ids = [aws_security_group.hensley_ratings.id]

  user_data = file("${path.module}/user_data.sh")

  tags = {
    Name = "hensley-ratings"
  }
}

resource "aws_eip" "hensley_ratings" {
  domain = "vpc"

  tags = {
    Name = "hensley-ratings-eip"
  }
}

resource "aws_eip_association" "hensley_ratings" {
  instance_id   = aws_instance.hensley_ratings.id
  allocation_id = aws_eip.hensley_ratings.id
}

data "aws_route53_zone" "hensley_ratings" {
  name         = var.domain_name
  private_zone = false
}

resource "aws_route53_record" "apex" {
  zone_id         = data.aws_route53_zone.hensley_ratings.zone_id
  name            = var.domain_name
  type            = "A"
  ttl             = 300
  records         = [aws_eip.hensley_ratings.public_ip]
  allow_overwrite = true
}

resource "aws_route53_record" "www" {
  zone_id         = data.aws_route53_zone.hensley_ratings.zone_id
  name            = "www.${var.domain_name}"
  type            = "A"
  ttl             = 300
  records         = [aws_eip.hensley_ratings.public_ip]
  allow_overwrite = true
}
