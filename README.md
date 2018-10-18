# Web Site Setup #

Version 1.0

This is a setup project template for WebSite projects. It includes database configuration and IIS configuration steps, supported by custom actions.

## Usage ##

Just clone, and run build.bat!

...
...
Unless...

Since accessing the IIS configuration requires user elevation, the installation msi cannot request it when starting the deploy transaction, so the whole msi is bootstrapped to request elevation from the get go.
The bootstrapper was created with VS 2013 and Windows SDK 10. C++ include and libraries paths might need to be updated if the project is run on a machine with different settings.
The VS property parameter in the build.bat file might need to be updated too.
NuGet packages will be downloaded and installed on first build.