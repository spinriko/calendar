# ASP.NET Core Resource Scheduling Calendar (Open-Source)

This is the code repository for the [ASP.NET Core Resource Scheduling Calendar (Open-Source)](https://code.daypilot.org/20604/asp-net-core-resource-calendar-open-source) tutorial by [DayPilot](https://www.daypilot.org/).

## Description
- Includes [DayPilot Lite for JavaScript](https://javascript.daypilot.org/open-source/) - open-source calendar/scheduling components for JavaScript/Angular/React/Vue (Apache License 2.0).
- This project was generated using the [DayPilot UI Builder](https://builder.daypilot.org/), an online tool for configuring DayPilot components and generating starter projects.

## License
- The code of this tutorial is licensed under Apache License 2.0.
- This tutorial may include third-party libraries available under their respective licenses.
## Deploying to Azure (SQLite)

This repository can be deployed to Azure App Service using a file-based SQLite database (cheapest option). This is suitable for development or low-traffic single-instance apps.

Quick steps:

- Build and publish the app locally:

```bash
dotnet publish Project/Project.csproj -c Release -o ./publish
cd publish
zip -r ../app.zip .
cd ..
```

- Provision an App Service (Free/S1) and Web App using `az` (example script provided in `scripts/azure-deploy-sqlite.sh`). Before running, replace `RESOURCE_GROUP`, `APP_NAME`, and `LOCATION` with your choices.

- The script sets the SQLite connection string to `Data Source=/home/site/wwwroot/App_Data/scheduler.db` and deploys the zip package.

Notes & caveats:

- SQLite stores the database file in the App Service file system. Do not scale out to multiple instances — SQLite is not safe to share between instances.
- Back up the SQLite file (`/home/site/wwwroot/App_Data/scheduler.db`) by copying it to Azure Blob Storage periodically.
- For production usage, prefer Azure SQL Database and set the connection string accordingly.

See `scripts/azure-deploy-sqlite.sh` for an example `az` script.
