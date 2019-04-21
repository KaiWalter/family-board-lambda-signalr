# Family or Kitchen Board based on Azure Functions and SignalR

> UNDER CONSTRUCTION

A family calendar / kitchen board which will one day show information

- from a Google Calendar
- from an Outlook Calendar
- random images from OneDrive

The app will be put into a container and deployed to Rasberry Pi W.

## Architecture

Front-end is based on static pages which are hosted from Consumption Plan Azure Functions (class `StaticFileServer`)- derived from this [blog by Anthony Chu](https://anthonychu.ca/post/azure-functions-static-file-server/).

For basic protection `index.html` can only be opened with a Azure Functions key: `https://{function-app}.azurewebsites.net/index.html?code={key}`. All other static content is not protected.

...to be continued...