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
    * [`DeferRegistrationWhenPackagesAreInUse : true`](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.addpackageoptions.deferregistrationwhenpackagesareinuse?view=winrt-22621#windows-management-deployment-addpackageoptions-deferregistrationwhenpackagesareinuse) is respected and package update is postponed until the service is in the stopped state.
    * Staged packaged services are not automatically registered, when the packaged service is in the `Running` state.

**Attempted workaround:**
* [`uap17:UpdateWhileInUse: defer`](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-uap17-updatewhileinuse) does not affect the behavior above (tested on the 10.0.26058, Insider Preview build).

## Steps to reproduce
The test `Installer_TemporaryKey.pfx` certificate is used to sign the installer projects (password `Test@123`).

1. Publish the `DesktopClient.Installer` project, for example, version `1.0.0.0`.
2. Publish the `WindowsService.Installer` project, for example, version `1.0.0.0`.
3. Publish the `WindowsService.Installer` project one more time, for example, version `2.0.0.0`.
4. Using PowerShell (running as admin), install the `WindowsService.Installer_1.0.0.0_x64_Debug.msixbundle`, for example by using the:
    * `Add-AppxPackage "WindowsService.Installer\AppPackages\WindowsService.Installer_1.0.0.0_Debug_Test\WindowsService.Installer_1.0.0.0_x64_Debug.msixbundle"`.
5. Start the installed Windows service, either using the Services UI or by using the following CMD command (not PowerShell) - `sc start TestWindowsService`.
    * Verify the service is running using the Services UI or by using the following CMD command (not PowerShell) - `sc query TestWindowsService`.
6. Defer the `WindowsService.Installer_2.0.0.0_x64_Debug.msixbundle` package update by using the following PowerShell command:
    * `Add-AppxPackage "WindowsService.Installer\AppPackages\WindowsService.Installer_2.0.0.0_Debug_Test\WindowsService.Installer_2.0.0.0_x64_Debug.msixbundle" -DeferRegistrationWhenPackagesAreInUse`
7. Verify that the `TestWindowsService` is still running.
8. Now provision the `DesktopClient.Installer_1.0.0.0_x64_Debug.msixbundle` package (this a package unrelated to the service):
    * First, stage: `Add-AppxPackage "DesktopClient.Installer\AppPackages\DesktopClient.Installer_1.0.0.0_Debug_Test\DesktopClient.Installer_1.0.0.0_x64_Debug.msixbundle" -Stage`.
    * Then, provision:
      * `$packageManager = [Windows.Management.Deployment.PackageManager]::new()` 
      * `$packageManager.ProvisionPackageForAllUsersAsync("DesktopClient_ek1grh0z258m2")`
