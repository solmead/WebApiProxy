param($installPath, $toolsPath, $package)

Import-Module (Join-Path $toolsPath "WebApiProxyCSharp.psm1") -DisableNameChecking
Import-Module (Join-Path $toolsPath "WebApiProxyTypeScript.psm1") -DisableNameChecking