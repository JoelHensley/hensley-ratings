output "elastic_ip" {
  description = "Elastic IP address of the EC2 instance"
  value       = aws_eip.hensley_ratings.public_ip
}

output "ssh_command" {
  description = "SSH command to connect to the instance"
  value       = "ssh -i ~/.ssh/hensley-ratings ec2-user@${aws_eip.hensley_ratings.public_ip}"
}
