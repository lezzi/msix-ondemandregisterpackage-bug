# Overview

This repository contains a reproducible example of a `PackageManager` issue that can unexpectedly terminate packaged services.

## Background
Running packaged services are terminated by the system when:
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
    * This is an example package containing an empty desktop app, in real life any package can be used.
2. Publish the `WindowsService.Installer` project, for example, version `1.0.0.0`.
    * This is a package containing a Windows service.
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
      * `$packageManager.ProvisionPackageForAllUsersAsync("DesktopClient_ek1grh0z258m2")` (family name can be different if a different certificate was used to sign the package)
9. Observe the `TestWindowsService` being in the `Stopped` state and the package being force-updated to the version `2.0.0.0`.


## Related Event Viewer events
* Logs from the Event Viewer -> Application and Service Logs -> Microsoft -> Windows -> AppxDeployment-Server:
  * `Started deployment OnDemandRegisterOperation operation on a package with main parameter , dependency parameters DesktopClient_1.0.0.0_neutral_~_ek1grh0z258m2, and Options ImmediatePriorityRequest and 0.`
  * `OnDemandRegisterPackage found existing package WindowsService_1.0.0.0_x64__ek1grh0z258m2, set PACKAGE_STATUS_REGISTRATION_REQUIRED_BLOCKING`
  * `Started deployment Register operation on a package with main parameter AppxBundleManifest.xml and Options ForceTargetApplicationShutdownOption,SkipReregisterIfPackageStatusOk,NormalPriorityRequest and SkipDeploymentOperationRpcCallerIsAdminCheck.`
  * `Deployment Register operation on package WindowsService_2.0.0.0_neutral_~_ek1grh0z258m2 has been de-queued and is running for user`
  * `0x0: TerminateApplications successful.`

See `ondemandregisterpacakge-terminate-packaged-service.evtx` for more info (recorded starting from step 6).
