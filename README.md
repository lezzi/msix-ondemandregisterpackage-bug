# Overview

This repository contains a reproducible example of a `PackageManager` issue that can unexpectedly terminate packaged services.

## Background
* Running packaged services are terminated by the system when:
  * A new packaged service update is deferred or staged.
    * Either by using [`PackageManager.AddPackageByUriAsync`](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.addpackagebyuriasync?view=winrt-22621) with [`DeferRegistrationWhenPackagesAreInUse : true`](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.addpackageoptions.deferregistrationwhenpackagesareinuse?view=winrt-22621#windows-management-deployment-addpackageoptions-deferregistrationwhenpackagesareinuse).
    * Or by using [`PackageManager.StagePackageByUriAsync`](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.stagepackagebyuriasync?view=winrt-22621).
* The [`PackageManager.ProvisionPackageForAllUsersAsync`](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.provisionpackageforallusersasync?view=winrt-22621) API is used to provision any unrelated package, including default Microsoft ones, like OneDrive.
  * **Actual behavior:** This API triggers the `OnDemandRegisterPackage`, which then marks packaged services as `PACKAGE_STATUS_REGISTRATION_REQUIRED_BLOCKING` and then starts their registration with the `ForceTargetApplicationShutdownOption` flag.
  * **Expected behavior:**
    * [`DeferRegistrationWhenPackagesAreInUse : true`](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.addpackageoptions.deferregistrationwhenpackagesareinuse?view=winrt-22621#windows-management-deployment-addpackageoptions-deferregistrationwhenpackagesareinuse) has to be respected and not lead to the running service termination.
    * Staged packaged services should not automatically register, when the packaged service is in the `Running` state.

**Attempted workaround:**
* [`uap17:UpdateWhileInUse: defer`](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-uap17-updatewhileinuse) does not affect the behavior above (tested on the 10.0.26058, Insider Preview build).

## Steps to reproduce
The test `Installer_TemporaryKey.pfx` certificate is used to sign the installer projects (password `Test@123`).
