# Family or Kitchen Board based on Azure Functions and SignalR

> UNDER CONSTRUCTION

A family calendar / kitchen board which shows information from

- from a Google Calendar
- from an Outlook Calendar
- random images from OneDrive

This app is access from a Rasberry Pi W an replaces the [family board previously used](https://www.hanselman.com/blog/HowToBuildAWallMountedFamilyCalendarAndDashboardWithARaspberryPiAndCheapMonitor.aspx).

## Architecture

Front-end is based on static pages which are hosted from Consumption Plan Azure Functions (class `StaticFileServer`)- derived from this [blog by Anthony Chu](https://anthonychu.ca/post/azure-functions-static-file-server/).

For basic protection `index.html` can only be opened with a Azure Functions key: `https://{function-app}.azurewebsites.net/index.html?code={key}`. All other static content is not protected.

...to be continued...

## Limitations

- currently only fix 3 weeks model
- only German weekdays from Monday to Sunday
