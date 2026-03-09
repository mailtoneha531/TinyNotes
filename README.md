# TinyNotes

A full-stack notes service with React frontend, .NET 10 backend, and PostgreSQL (Docker).

## Features
- Complete CRUD for notes
- Search and sort notes
- OpenTelemetry logging
- Optional: AI summary and hashtags

## Project Structure
- frontend: React + TypeScript
- backend: .NET 10 Web API
- docker-compose.yml: PostgreSQL setup

## Quick Start

1. Start PostgreSQL:
   ```bash
   docker-compose up -d
   ```

2. Backend:
   ```bash
   cd backend
   dotnet run
   ```

3. Frontend:
   ```bash
   cd frontend
   npm start
   ```

## Database Defaults
- User: notes_user
- Password: notes_pass
- Database: notes_db
- Host: localhost
- Port: 5432

## Deployment
- AWS CDK or CloudFormation template (optional)

# CloudFormation Template for Deployment

This project can be deployed using the provided CloudFormation template. The template sets up the following AWS resources:

- **Amazon ECS**: Hosts the backend application.
- **Amazon RDS**: Provides a PostgreSQL database for the backend.
- **Amazon S3**: Hosts the static frontend files.

## Steps to Deploy

1. **Upload the CloudFormation Template**:
   - Navigate to the AWS Management Console.
   - Go to the CloudFormation service.
   - Create a new stack and upload the provided template.

2. **Provide Parameters**:
   - Specify the required parameters such as database credentials, ECS cluster name, etc.

3. **Deploy the Stack**:
   - Review the stack configuration and deploy it.

4. **Access the Application**:
   - Once the stack is deployed, note the output values for the application URL and database endpoint.

## Notes
- Ensure that you have the necessary IAM permissions to create the resources.
- Replace placeholders in the template with actual values before deployment.

## Logging
- OpenTelemetry (backend)

---

For more details, see copilot-instructions.md in .github.
