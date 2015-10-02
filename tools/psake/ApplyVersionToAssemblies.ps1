##-----------------------------------------------------------------------
## <copyright file="ApplyVersionToAssemblies.ps1">(c) Microsoft Corporation. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
##-----------------------------------------------------------------------
# Look for a 0.0.0.0 pattern in the build number. 
# If found use it to version the assemblies.
#
# For example, if the 'Build number format' build process parameter 
# $(BuildDefinitionName)_$(Year:yyyy).$(Month).$(DayOfMonth)$(Rev:.r)
# then your build numbers come out like this:
# "Build HelloWorld_2013.07.19.1"
# This script would then apply version 2013.07.19.1 to your assemblies.

# Enable -Verbose option
[CmdletBinding()]

# Regular expression pattern to find the version in the build number 
# and then apply it to the assemblies
$VersionRegex = "\d+\.\d+\.\d+\.\d+"

# Make sure path to source code directory is available
if (-not $Env:APPVEYOR_BUILD_FOLDER)
{
    Write-Error ("APPVEYOR_BUILD_FOLDER environment variable is missing.")
    exit 1
}
elseif (-not (Test-Path $Env:APPVEYOR_BUILD_FOLDER))
{
    Write-Error "APPVEYOR_BUILD_FOLDER does not exist: $Env:APPVEYOR_BUILD_FOLDER"
    exit 1
}
Write-Verbose "APPVEYOR_BUILD_FOLDER: $Env:APPVEYOR_BUILD_FOLDER"

# Make sure there is a build number
if (-not $Env:APPVEYOR_BUILD_VERSION)
{
    Write-Error ("APPVEYOR_BUILD_VERSION environment variable is missing.")
    exit 1
}
Write-Verbose "APPVEYOR_BUILD_VERSION: $Env:APPVEYOR_BUILD_VERSION"

# Get and validate the version data
$VersionData = [regex]::matches($Env:APPVEYOR_BUILD_VERSION,$VersionRegex)
switch($VersionData.Count)
{
   0        
      { 
         Write-Error "Could not find version number data in APPVEYOR_BUILD_VERSION."
         exit 1
      }
   1 {}
   default 
      { 
         Write-Warning "Found more than instance of version data in APPVEYOR_BUILD_VERSION." 
         Write-Warning "Will assume first instance is version."
      }
}
$NewVersion = $VersionData[0]
Write-Verbose "Version: $NewVersion"

# Apply the version to the assembly property files
$files = gci $Env:APPVEYOR_BUILD_FOLDER -recurse  | 
    ?{ $_.PSIsContainer } | 
    foreach { gci -Path $_.FullName -Recurse -include AssemblyInfo.* }

if($files)
{
    Write-Verbose "Will apply $NewVersion to $($files.count) files."

    foreach ($file in $files) {
        $filecontent = Get-Content($file)
        attrib $file -r
        $filecontent -replace $VersionRegex, $NewVersion | Out-File $file
        Write-Verbose "$file.FullName - version applied"
    }
}

$common = gci $Env:APPVEYOR_BUILD_FOLDER\"CommonAssemblyInfo.cs"
if ($common)
{
    $filecontent = Get-Content($common)
    attrib $common -r
    $filecontent -replace $VersionRegex, $NewVersion | Out-File $common
    Write-Verbose "$common.FullName - version applied"
}