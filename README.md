# Up for grabs feed

Docker container for creating projects.json file which is pushed to an Azure CDN. The source of the data comes from up-for-grabs.net in 600+ YAML files, container converts to JSON and combines into a single file.

Azure DevOps is used to build the container, push to Azure Container Registry and deploy to Azure Container Instances to complete the job. CI is triggered by commits to this repository as well as scheduled nighlty builds to get the projects commited to the up-for-grabs.net site.

This is a source data file exposing projects that are looking for first timers and good first issues on open source projects.
