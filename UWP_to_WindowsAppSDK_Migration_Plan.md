# UWP to Windows App SDK (WinUI 3) Migration Plan
**Kiosk Client Application**

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Migration Strategy](#2-migration-strategy)
3. [Detailed Dependency Analysis](#3-detailed-dependency-analysis)
4. [Phase 0: Preparation & Prerequisites](#4-phase-0-preparation--prerequisites)
5. [Phase 1: Project Structure Modernization](#5-phase-1-project-structure-modernization)
6. [Phase 2: Dependency Updates](#6-phase-2-dependency-updates)
7. [Phase 3: Background Task Re-Architecture](#7-phase-3-background-task-re-architecture)
8. [Phase 4: UI Migration (XAML & Code-Behind)](#8-phase-4-ui-migration-xaml--code-behind)
9. [Phase 5: Packaging & Manifest Updates](#9-phase-5-packaging--manifest-updates)
10. [Phase 6: Testing & Validation](#10-phase-6-testing--validation)
11. [Phase 7: Store Submission & Deployment](#11-phase-7-store-submission--deployment)
12. [Risk Management](#12-risk-management)
13. [Testing & Validation Strategy](#13-testing--validation-strategy)
14. [Complexity & Effort Assessment](#14-complexity--effort-assessment)
15. [Source Control Strategy](#15-source-control-strategy)
16. [Success Criteria](#16-success-criteria)

---

## 1. Executive Summary

### Scenario Description
Migrate the **Kiosk Client** application from **Universal Windows Platform (UWP)** to **Windows App SDK (WinUI 3)** with modern **.NET 8.0**, enabling continued Microsoft Store distribution and full kiosk mode support while modernizing the technology stack.

### Current State
- **Platform**: UWP targeting Windows 10 SDK 10.0.18362.0 (min: 10.0.17763.0)
- **Framework**: .NET Native with UWP runtime
- **Project Style**: Old-style .csproj files
- **Projects**: 3 source projects + 4 test projects (7 total)
- **Key Features**: Kiosk mode, background polling, Microsoft Store distribution
- **Status**: Projects failing to load due to missing UWP SDK dependencies

### Target State
- **Platform**: Windows App SDK 1.6+
- **Framework**: .NET 8.0 (net8.0-windows10.0.19041.0)
- **Project Style**: SDK-style .csproj files
- **Projects**: Modernized with updated dependencies
- **Key Features**: All features preserved (kiosk mode, background tasks, Store distribution)

### Scope
**Projects to Migrate**:
1. `KioskLibrary` (UWP Class Library → .NET 8 Class Library)
2. `OrchestrationPollingManager` (UWP Runtime Component → .NET 8 Class Library / Windows Service)
3. `KioskClient` (UWP App → Windows App SDK WinUI 3 App)
4. `CommonTestLibrary` (UWP Test Library → .NET 8 xUnit/MSTest)
5. `KioskClient.Spec` (UWP Test → .NET 8 xUnit/MSTest)
6. `KioskLibrary.Spec` (UWP Test → .NET 8 xUnit/MSTest)
7. `OrchestrationPollingManager.Spec` (UWP Test → .NET 8 xUnit/MSTest)

### Selected Strategy
**Incremental, Dependency-Ordered Migration** with phased rollout.

**Rationale**:
- **7 projects** with complex interdependencies
- **Background task re-architecture** is high-risk and requires isolated validation
- **Kiosk mode preservation** is mission-critical - incremental testing reduces risk
- **Store distribution continuity** - phased approach allows validation at each step
- **Team learning curve** - staged migration allows knowledge building

### Complexity Assessment
- **Overall Complexity**: **MEDIUM-HIGH**
- **Critical Challenge**: Background task re-architecture (HIGH complexity)
- **Total Estimated Effort**: **60-94 hours** (1.5 to 2.5 developer-weeks)
- **Risk Level**: **MEDIUM** with proper mitigation strategies

### Critical Issues
1. **Background Tasks**: UWP `IBackgroundTask` incompatible with Windows App SDK - requires complete redesign
2. **XAML Namespace Changes**: All UI files require namespace updates (Windows.UI.Xaml → Microsoft.UI.Xaml)
3. **AppCenter Compatibility**: Microsoft AppCenter SDK may need replacement or verification
4. **Project Loading**: Current projects won't load without UWP SDK installation

### Recommended Approach
Execute **7 phases** over **3-4 weeks** with dependency-ordered project migration, comprehensive testing after each phase, and parallel preparation of alternative approaches for high-risk components (background tasks).

---

## 2. Migration Strategy

### Approach Selection: Incremental Migration

**Why Incremental?**
- ✅ **Risk Mitigation**: Validate each project before proceeding to dependents
- ✅ **Dependency Safety**: Projects migrate after their dependencies complete
- ✅ **Testing Isolation**: Each phase can be tested independently
- ✅ **Rollback Capability**: Can revert individual phases if issues arise
- ✅ **Knowledge Building**: Team learns Windows App SDK incrementally
- ✅ **Background Task Complexity**: High-risk component isolated for focused attention

**Why NOT All-at-Once?**
- ❌ Background task re-architecture is HIGH risk - needs isolated validation
- ❌ 7 projects with complex dependencies - too much simultaneous change
- ❌ Kiosk mode is mission-critical - incremental validation required
- ❌ Limited team experience with Windows App SDK - learning curve considerations

### Migration Phases Overview

```
Phase 0: Preparation (3-5 days)
├─ Install Windows App SDK tooling
├─ Create feature branch
├─ Verify dependency compatibility
└─ Set up test environment

Phase 1: Project Structure Modernization (5-8 days)
├─ Tier 1: KioskLibrary (leaf node, no project dependencies)
├─ Tier 2: OrchestrationPollingManager (depends on KioskLibrary)
└─ Tier 3: KioskClient (depends on both libraries)

Phase 2: Dependency Updates (4-6 days)
├─ Replace UWP NuGet packages with Windows App SDK equivalents
├─ Update CommunityToolkit packages
└─ Modernize System.* package references

Phase 3: Background Task Re-Architecture (3-5 days)
├─ Research Windows App SDK background task capabilities
├─ Design alternative architecture (Windows Service / App Service)
├─ Implement and test new background polling mechanism
└─ Validate orchestration update functionality

Phase 4: UI Migration (4-6 days)
├─ Update App.xaml / App.xaml.cs
├─ Migrate XAML namespace references
├─ Update pages and dialogs
└─ Fix code-behind for WinUI 3

Phase 5: Packaging & Manifest (2-3 days)
├─ Update Package.appxmanifest to Windows App SDK schema
├─ Verify MSIX packaging
└─ Test kiosk mode extensions

Phase 6: Testing & Validation (5-7 days)
├─ Update test projects
├─ Functional testing
├─ Kiosk mode validation
└─ Performance testing

Phase 7: Store Submission (2-3 days)
├─ Package for Store
├─ Run WACK validation
└─ Submit to Microsoft Store
```

### Dependency-Based Ordering Rationale

**Migration Order**:
1. **KioskLibrary** (Tier 1 - Leaf Node)
   - No project dependencies
   - Depended upon by both OrchestrationPollingManager and KioskClient
   - **Must migrate first**

2. **OrchestrationPollingManager** (Tier 2 - Middle Layer)
   - Depends on: KioskLibrary
   - Depended upon by: KioskClient
   - **Migrates after KioskLibrary, before KioskClient**

3. **KioskClient** (Tier 3 - Root Application)
   - Depends on: KioskLibrary, OrchestrationPollingManager
   - Depended upon by: Nothing (entry point)
   - **Migrates last**

4. **Test Projects** (Tier 4 - After All Source Projects)
   - Depend on source projects
   - Migrate after source projects stabilize

**Critical Constraint**: A project cannot migrate until all its dependencies have successfully migrated.

### Parallel vs Sequential Execution

**Sequential Execution Required** (No Parallelization):
- Each tier must complete before next tier begins
- **Reason**: Projects depend on previous tier's completion
- **Exception**: Test projects within Tier 4 can potentially be migrated in parallel

### Phase Definitions

| Phase | Goal | Deliverable | Validation Criteria |
|-------|------|-------------|---------------------|
| **0: Preparation** | Environment setup | Development environment ready | Windows App SDK sample builds successfully |
| **1: Project Structure** | Modernize .csproj files | All projects use SDK-style format, target .NET 8 | Projects load in Visual Studio, NuGet restore succeeds |
| **2: Dependencies** | Update packages | Windows App SDK packages installed | No package conflicts, build succeeds |
| **3: Background Tasks** | Re-architect polling | New background mechanism functional | Polling works, orchestration updates succeed |
| **4: UI Migration** | WinUI 3 conversion | All XAML updated, app launches | Application starts, all pages navigate |
| **5: Packaging** | MSIX generation | Valid Windows App SDK package | MSIX installs, kiosk mode configurable |
| **6: Testing** | Comprehensive validation | All tests pass | Functional, kiosk, performance tests pass |
| **7: Deployment** | Store submission | App live in Store | Certification passes, app downloadable |

---

## 3. Detailed Dependency Analysis

### Project Dependency Graph

```
KioskClient (Main App)
├── depends on → KioskLibrary
└── depends on → OrchestrationPollingManager
    └── depends on → KioskLibrary

CommonTestLibrary (Test Utilities)
└── (no project dependencies - only NuGet)

KioskClient.Spec (Tests)
├── depends on → KioskClient
└── depends on → CommonTestLibrary

KioskLibrary.Spec (Tests)
├── depends on → KioskLibrary
└── depends on → CommonTestLibrary

OrchestrationPollingManager.Spec (Tests)
├── depends on → OrchestrationPollingManager
└── depends on → CommonTestLibrary
```

### Migration Tiers

#### **Tier 1: Foundation Libraries (Migrate First)**
- ✅ **KioskLibrary**
  - **Dependencies**: None (only NuGet packages)
  - **Depended Upon By**: OrchestrationPollingManager, KioskClient
  - **Complexity**: MEDIUM
  - **Risk**: LOW-MEDIUM
  - **Key Changes**: SDK-style project, .NET 8 target, update NuGet packages
  - **Estimated Effort**: 8-12 hours

#### **Tier 2: Background Task Layer (Migrate Second)**
- ✅ **OrchestrationPollingManager**
  - **Dependencies**: KioskLibrary
  - **Depended Upon By**: KioskClient
  - **Complexity**: HIGH (background task re-architecture)
  - **Risk**: HIGH
  - **Key Changes**: Complete redesign of background task mechanism
  - **Estimated Effort**: 16-24 hours

#### **Tier 3: Application Layer (Migrate Third)**
- ✅ **KioskClient**
  - **Dependencies**: KioskLibrary, OrchestrationPollingManager
  - **Depended Upon By**: None (entry point)
  - **Complexity**: MEDIUM-HIGH (extensive XAML updates)
  - **Risk**: MEDIUM
  - **Key Changes**: WinUI 3 migration, XAML namespace updates, manifest updates
  - **Estimated Effort**: 20-30 hours

#### **Tier 4: Test Projects (Migrate Last)**
- ✅ **CommonTestLibrary** (foundational test utilities)
  - **Dependencies**: None
  - **Complexity**: LOW
  - **Effort**: 2-4 hours

- ✅ **KioskLibrary.Spec**
  - **Dependencies**: KioskLibrary, CommonTestLibrary
  - **Complexity**: LOW-MEDIUM
  - **Effort**: 3-5 hours

- ✅ **OrchestrationPollingManager.Spec**
  - **Dependencies**: OrchestrationPollingManager, CommonTestLibrary
  - **Complexity**: MEDIUM (tests for complex component)
  - **Effort**: 4-6 hours

- ✅ **KioskClient.Spec**
  - **Dependencies**: KioskClient, CommonTestLibrary
  - **Complexity**: MEDIUM
  - **Effort**: 4-6 hours

### Critical Path Identification

**Critical Path** (longest dependency chain):
```
KioskLibrary → OrchestrationPollingManager → KioskClient → KioskClient.Spec
```

**Total Critical Path Effort**: ~51-72 hours (accounts for bulk of migration)

**Timeline Impact**: Sequential execution along critical path determines minimum project duration (~3 weeks with testing/validation).

### Circular Dependency Analysis

✅ **No circular dependencies detected**

All dependencies flow in a single direction (libraries → app → tests), enabling clean sequential migration.

---

## 4. Phase 0: Preparation & Prerequisites

**Duration**: 3-5 days  
**Complexity**: LOW  
**Prerequisite**: None

### Objectives
- Set up development environment with Windows App SDK tooling
- Create isolated feature branch for migration work
- Verify third-party package compatibility
- Establish baseline test results
- Prepare rollback plan

### Detailed Steps

#### Step 0.1: Install Development Prerequisites

**Required Software**:
1. **Visual Studio 2022** (version 17.8 or later)
   - Workloads:
     - .NET Desktop Development
     - Universal Windows Platform Development (for comparison/reference)
     - Windows App SDK development
   - Individual Components:
     - .NET 8.0 SDK
     - Windows 11 SDK (10.0.22621.0 or later)

2. **Windows App SDK** (version 1.6 or later)
   - Install via Visual Studio Installer or standalone installer
   - Verify installation: Create test WinUI 3 app project

3. **Additional Tools**:
   - Windows App Certification Kit (WACK) for package validation
   - Git for version control
   - Windows Performance Toolkit (optional, for performance testing)

**Validation**:
- [ ] Visual Studio 2022 launches successfully
- [ ] Can create new "Blank App, Packaged (WinUI 3 in Desktop)" project
- [ ] Test WinUI 3 project builds and runs
- [ ] .NET 8.0 SDK is installed (`dotnet --list-sdks` shows 8.0.x)

#### Step 0.2: Verify Third-Party Package Compatibility

Research and verify compatibility of current NuGet packages:

**Critical Packages to Verify**:
1. **Microsoft.AppCenter** (5.0.7)
   - [ ] Check Windows App SDK compatibility
   - [ ] Research latest version supporting .NET 8
   - [ ] Identify alternative if incompatible (e.g., Application Insights)

2. **CommunityToolkit.Uwp.Controls.SettingsControls** (8.2.250402)
   - [ ] Confirm `CommunityToolkit.WinUI.Controls` equivalent exists
   - [ ] Verify feature parity for SettingsControls
   - [ ] Note any API differences

3. **Serilog** (4.3.0)
   - [ ] Verify .NET 8 compatibility (expected: ✅ compatible)
   - [ ] Check file sink compatibility

4. **Newtonsoft.Json** (13.0.4)
   - [ ] Verify .NET 8 compatibility (expected: ✅ compatible)
   - [ ] Consider migration to `System.Text.Json` (optional modernization)

**Deliverable**: Compatibility matrix document listing all packages and their Windows App SDK equivalents.

#### Step 0.3: Create Feature Branch

Create isolated branch for migration work:

```bash
# Ensure working directory is clean
git status

# Create and switch to feature branch
git checkout -b feature/windows-app-sdk-migration

# Push branch to remote
git push -u origin feature/windows-app-sdk-migration
```

**Branch Protection**:
- [ ] Configure branch as protected (require PR for merge to main)
- [ ] Set up CI build for feature branch (if applicable)
- [ ] Document branch purpose in project management system

#### Step 0.4: Baseline Documentation

**Capture Current State**:

1. **Build Current Solution** (if UWP SDKs available):
   - [ ] Document build output (errors, warnings, success)
   - [ ] Capture any existing issues
   - [ ] Screenshot Solution Explorer showing project structure

2. **Run Existing Tests** (if possible):
   - [ ] Execute all test projects
   - [ ] Document pass/fail counts
   - [ ] Capture baseline performance metrics

3. **Document Current Functionality**:
   - [ ] List all features in KioskClient
   - [ ] Document kiosk mode configuration process
   - [ ] Capture current background task polling behavior
   - [ ] Screenshot current UI (MainPage, Settings, About, etc.)

**Deliverable**: `migration-baseline.md` document with current state details.

#### Step 0.5: Set Up Test Environment

**Kiosk Mode Testing Device**:
- [ ] Provision Windows 11 test device (physical or VM)
- [ ] Configure test device for sideloading
- [ ] Install current MSIX package (if available)
- [ ] Configure Assigned Access (kiosk mode)
- [ ] Document current kiosk mode behavior

**Development Testing**:
- [ ] Set up debugging workflow for Windows App SDK apps
- [ ] Configure test data / orchestration endpoints
- [ ] Prepare sample orchestration JSON files

#### Step 0.6: Prepare Rollback Plan

**Backup Strategy**:
```bash
# Create backup branch from current state
git checkout main
git checkout -b backup/pre-winappssdk-migration
git push -u origin backup/pre-winappssdk-migration

# Tag current release
git tag -a v1.2.0-uwp-final -m "Final UWP version before Windows App SDK migration"
git push origin v1.2.0-uwp-final
```

**Rollback Procedure Documentation**:
- [ ] Document steps to revert to UWP version
- [ ] Identify merge commits for each phase (for selective rollback)
- [ ] Prepare communication plan if rollback needed

### Validation Checklist

**Phase 0 Complete When**:
- [ ] Visual Studio 2022 with Windows App SDK installed and verified
- [ ] Compatibility matrix for all NuGet packages completed
- [ ] Feature branch created and pushed
- [ ] Baseline documentation captured
- [ ] Test environment configured
- [ ] Rollback plan documented
- [ ] Test WinUI 3 sample project builds and runs successfully

**Estimated Effort**: 16-20 hours

---

## 5. Phase 1: Project Structure Modernization

**Duration**: 5-8 days  
**Complexity**: MEDIUM-HIGH  
**Prerequisite**: Phase 0 complete

### Objectives
- Convert all projects from old-style .csproj to SDK-style .csproj
- Retarget all projects to .NET 8.0 with appropriate Windows SDK targets
- Update project references and file inclusions
- Ensure all projects load successfully in Visual Studio 2022
- Achieve successful NuGet package restore for all projects

### Migration Order (Tier-Based)

#### Tier 1: KioskLibrary (Migrate First)
#### Tier 2: OrchestrationPollingManager (Migrate Second)
#### Tier 3: KioskClient (Migrate Third)
#### Tier 4: Test Projects (Migrate Last)

---

### Step 1.1: Migrate KioskLibrary to SDK-Style Project

**Current State**:
- Old-style .csproj with `ToolsVersion="15.0"`
- Target: UWP (`TargetPlatformIdentifier=UAP`)
- Output: Class Library for UWP
- Dependencies: Microsoft.NETCore.UniversalWindowsPlatform 6.2.14

**Target State**:
- SDK-style .csproj
- Target: `net8.0-windows10.0.19041.0`
- Output: .NET 8 Class Library
- Dependencies: Windows App SDK compatible packages

**Detailed Steps**:

1. **Backup Original Project File**:
   ```bash
   cd src/KioskLibrary
   copy KioskLibrary.csproj KioskLibrary.csproj.uwp.backup
   ```

2. **Create New SDK-Style Project File**:

   Replace entire `KioskLibrary.csproj` content with:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
       <OutputType>Library</OutputType>
       <RootNamespace>KioskLibrary</RootNamespace>
       <AssemblyName>KioskLibrary</AssemblyName>
       <Platforms>x86;x64;ARM64</Platforms>
       <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
       <UseWinUI>true</UseWinUI>
       <EnableMsixTooling>true</EnableMsixTooling>
       <LangVersion>12.0</LangVersion>
       <Nullable>enable</Nullable>
     </PropertyGroup>

     <ItemGroup>
       <!-- Windows App SDK -->
       <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250106001" />

       <!-- Existing compatible packages -->
       <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
       <PackageReference Include="Serilog" Version="4.3.0" />
       <PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
     </ItemGroup>
   </Project>
   ```

3. **Remove UWP-Specific Files**:
   - Delete `Properties/Default.rd.xml` (UWP reflection configuration)
   - Delete any `.pfx` certificate files from library project

4. **Update Code Files**:

   **File: `Storage/ApplicationStorage.cs`**
   - Update namespace: `Windows.Storage` → `Windows.Storage` (no change needed, still available)
   - Verify `ApplicationData.Current.LocalSettings` still available in Windows App SDK

   **File: `IsExternalInit.cs`**
   - May no longer be needed with .NET 8 (includes init-only setters natively)
   - Remove if redundant, or keep for compatibility

5. **Build and Validate**:
   ```bash
   cd src/KioskLibrary
   dotnet restore
   dotnet build
   ```

   **Expected Warnings to Address**:
   - Nullable reference warnings (if Nullable enabled)
   - Obsolete API warnings
   - Package compatibility warnings

6. **Commit Changes**:
   ```bash
   git add src/KioskLibrary/KioskLibrary.csproj
   git add src/KioskLibrary/*.cs
   git commit -m "Migrate KioskLibrary to .NET 8 SDK-style project"
   ```

**Validation Checklist**:
- [ ] Project loads in Visual Studio without errors
- [ ] NuGet restore completes successfully
- [ ] `dotnet build` succeeds with 0 errors
- [ ] All source files included automatically (no explicit Compile items needed)
- [ ] Assembly output generated in `bin/` folder

---

### Step 1.2: Migrate OrchestrationPollingManager to SDK-Style Project

**Current State**:
- Old-style .csproj
- Target: UWP Windows Runtime Component (`OutputType=winmdobj`)
- Implements: `IBackgroundTask` interface

**Target State**:
- SDK-style .csproj
- Target: `net8.0-windows10.0.19041.0`
- Output: Class Library (background task redesign happens in Phase 3)

**Detailed Steps**:

1. **Backup Original Project File**:
   ```bash
   cd src/OrchestrationPollingManager
   copy OrchestrationPollingManager.csproj OrchestrationPollingManager.csproj.uwp.backup
   ```

2. **Create New SDK-Style Project File**:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
       <OutputType>Library</OutputType>
       <RootNamespace>OrchestrationPollingManager</RootNamespace>
       <AssemblyName>OrchestrationPollingManager</AssemblyName>
       <Platforms>x86;x64;ARM64</Platforms>
       <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
       <UseWinUI>true</UseWinUI>
       <EnableMsixTooling>true</EnableMsixTooling>
       <LangVersion>12.0</LangVersion>
       <Nullable>enable</Nullable>
     </PropertyGroup>

     <ItemGroup>
       <!-- Windows App SDK -->
       <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250106001" />

       <!-- Existing packages -->
       <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
       <PackageReference Include="Serilog" Version="4.3.0" />
       <PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
     </ItemGroup>

     <ItemGroup>
       <!-- Project reference to KioskLibrary -->
       <ProjectReference Include="..\KioskLibrary\KioskLibrary.csproj" />
     </ItemGroup>
   </Project>
   ```

3. **Temporary Code Changes** (Background task redesign deferred to Phase 3):

   **File: `OrchestrationUpdateTask.cs`**
   - **Temporarily comment out** UWP background task code
   - Add `#warning` comment noting Phase 3 will redesign this

   ```csharp
   // OrchestrationUpdateTask.cs
   namespace OrchestrationPollingManager
   {
       #warning TODO: Phase 3 - Re-architect background task for Windows App SDK

       // Temporarily disabled UWP background task implementation
       // Will be redesigned in Phase 3 using Windows App SDK approach

       /*
       public sealed class OrchestrationUpdateTask : IBackgroundTask
       {
           // ... existing code ...
       }
       */

       // Placeholder class to maintain namespace structure
       public class OrchestrationUpdateTask
       {
           // To be implemented in Phase 3
       }
   }
   ```

4. **Build and Validate**:
   ```bash
   cd src/OrchestrationPollingManager
   dotnet restore
   dotnet build
   ```

5. **Commit Changes**:
   ```bash
   git add src/OrchestrationPollingManager/
   git commit -m "Migrate OrchestrationPollingManager to .NET 8 SDK-style project (background task redesign pending Phase 3)"
   ```

**Validation Checklist**:
- [ ] Project loads in Visual Studio
- [ ] NuGet restore succeeds
- [ ] `dotnet build` succeeds (with warnings about Phase 3 work)
- [ ] ProjectReference to KioskLibrary resolves

---

### Step 1.3: Migrate KioskClient to SDK-Style WinUI 3 Project

**Current State**:
- Old-style .csproj
- Target: UWP Application (`AppContainerExe`)
- UI Framework: UWP XAML with WinUI 2.8.7

**Target State**:
- SDK-style .csproj
- Target: `net8.0-windows10.0.19041.0`
- UI Framework: WinUI 3 (Windows App SDK)
- Output: Packaged Windows App SDK application

**Detailed Steps**:

1. **Backup Original Project File**:
   ```bash
   cd src/KioskClient
   copy KioskClient.csproj KioskClient.csproj.uwp.backup
   ```

2. **Create New SDK-Style WinUI 3 Project File**:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
       <OutputType>WinExe</OutputType>
       <RootNamespace>KioskClient</RootNamespace>
       <AssemblyName>Kiosk-Client</AssemblyName>
       <Platforms>x86;x64;ARM64</Platforms>
       <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
       <UseWinUI>true</UseWinUI>
       <EnableMsixTooling>true</EnableMsixTooling>
       <WindowsPackageType>MSIX</WindowsPackageType>
       <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
       <LangVersion>12.0</LangVersion>
       <Nullable>enable</Nullable>
       <ApplicationManifest>app.manifest</ApplicationManifest>
     </PropertyGroup>

     <ItemGroup>
       <!-- Windows App SDK -->
       <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250106001" />
       <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1742" />

       <!-- CommunityToolkit for WinUI 3 -->
       <PackageReference Include="CommunityToolkit.WinUI.Controls.SettingsControls" Version="8.1.240916" />

       <!-- Existing packages -->
       <PackageReference Include="Humanizer.Core" Version="2.14.1" />
       <PackageReference Include="Serilog" Version="4.3.0" />
       <PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
       <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />

       <!-- AppCenter - TO VERIFY in Phase 2 -->
       <PackageReference Include="Microsoft.AppCenter" Version="5.0.7" />
       <PackageReference Include="Microsoft.AppCenter.Analytics" Version="5.0.7" />
       <PackageReference Include="Microsoft.AppCenter.Crashes" Version="5.0.7" />

       <Manifest Include="$(ApplicationManifest)" />
     </ItemGroup>

     <ItemGroup>
       <!-- Project References -->
       <ProjectReference Include="..\KioskLibrary\KioskLibrary.csproj" />
       <ProjectReference Include="..\OrchestrationPollingManager\OrchestrationPollingManager.csproj" />
     </ItemGroup>

     <ItemGroup>
       <!-- Assets (auto-included, but can specify explicitly if needed) -->
       <Content Include="Assets\**" />
       <Content Include="Resources\**" />
     </ItemGroup>
   </Project>
   ```

3. **Create app.manifest File** (required for Windows App SDK):

   Create `src/KioskClient/app.manifest`:

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
     <assemblyIdentity version="1.0.0.0" name="KioskClient.app"/>
     <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
       <application>
         <!-- Windows 10 version 1809 (minimum) -->
         <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
       </application>
     </compatibility>
     <application xmlns="urn:schemas-microsoft-com:asm.v3">
       <windowsSettings>
         <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
         <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
       </windowsSettings>
     </application>
   </assembly>
   ```

4. **Temporarily Stub Out XAML References** (detailed migration in Phase 4):

   **No changes to XAML yet** - just ensure project structure is correct. XAML namespace migration happens in Phase 4.

5. **Build and Validate** (expect errors - Phase 4 will fix):
   ```bash
   cd src/KioskClient
   dotnet restore
   dotnet build
   ```

   **Expected Build Errors** (to be fixed in Phase 4):
   - XAML namespace errors (`Windows.UI.Xaml` not found)
   - Code-behind compilation errors
   - Manifest schema errors

   **This is expected** - Phase 1 goal is project structure only.

6. **Commit Changes**:
   ```bash
   git add src/KioskClient/KioskClient.csproj
   git add src/KioskClient/app.manifest
   git commit -m "Migrate KioskClient to .NET 8 WinUI 3 SDK-style project (XAML migration pending Phase 4)"
   ```

**Validation Checklist**:
- [ ] Project loads in Visual Studio (may show errors)
- [ ] NuGet restore succeeds
- [ ] Project references to KioskLibrary and OrchestrationPollingManager resolve
- [ ] Expected build errors documented

---

### Step 1.4: Migrate Test Projects to .NET 8

**Test Projects to Migrate**:
1. CommonTestLibrary
2. KioskLibrary.Spec
3. OrchestrationPollingManager.Spec
4. KioskClient.Spec

**Strategy**: Convert to .NET 8 test projects using xUnit or MSTest (determine based on current test framework)

**Detailed Steps** (example for CommonTestLibrary, repeat for others):

1. **Backup Original**:
   ```bash
   cd test/Common/CommonTestLibrary
   copy CommonTestLibrary.csproj CommonTestLibrary.csproj.uwp.backup
   ```

2. **Create SDK-Style Test Project**:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
       <IsPackable>false</IsPackable>
       <IsTestProject>true</IsTestProject>
       <LangVersion>12.0</LangVersion>
       <Nullable>enable</Nullable>
     </PropertyGroup>

     <ItemGroup>
       <!-- Test Framework (adjust based on current framework) -->
       <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
       <PackageReference Include="xunit" Version="2.9.2" />
       <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
         <PrivateAssets>all</PrivateAssets>
         <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
       </PackageReference>

       <!-- Common test utilities -->
       <PackageReference Include="FluentAssertions" Version="6.12.2" />
       <PackageReference Include="Moq" Version="4.20.72" />
     </ItemGroup>
   </Project>
   ```

3. **Update Test Code** (if needed):
   - Replace UWP-specific test attributes with xUnit/MSTest equivalents
   - Update test initialization if using `[TestClass]` → `public class`

4. **Build and Validate**:
   ```bash
   dotnet restore
   dotnet build
   dotnet test
   ```

5. **Repeat for Remaining Test Projects**:
   - KioskLibrary.Spec (add `<ProjectReference>` to KioskLibrary and CommonTestLibrary)
   - OrchestrationPollingManager.Spec (add references)
   - KioskClient.Spec (add references - will have errors until Phase 4)

6. **Commit Changes**:
   ```bash
   git add test/
   git commit -m "Migrate test projects to .NET 8 xUnit test projects"
   ```

**Validation Checklist**:
- [ ] All test projects load in Visual Studio
- [ ] NuGet restore succeeds for all test projects
- [ ] `dotnet build` succeeds for test projects (except KioskClient.Spec may error)
- [ ] `dotnet test` can execute tests (may have failures due to incomplete migration)

---

### Step 1.5: Update Solution File

**Update KioskClient.sln** to reflect new project configurations:

1. **Open Solution in Visual Studio 2022**
2. **Verify All Projects Load**:
   - KioskLibrary ✅
   - OrchestrationPollingManager ✅ (with warnings)
   - KioskClient ⚠️ (with errors - expected)
   - All test projects ✅

3. **Remove UWP Platform Configurations**:
   - Remove "ARM" configuration (use ARM64 instead)
   - Keep: x86, x64, ARM64

4. **Update Build Configurations**:
   - Ensure "Any CPU" maps to x64 for Windows App SDK projects

5. **Save Solution File**

6. **Commit Changes**:
   ```bash
   git add KioskClient.sln
   git commit -m "Update solution file for Windows App SDK project configurations"
   ```

---

### Validation Checklist (Phase 1 Complete When)

- [ ] All 7 projects converted to SDK-style .csproj
- [ ] All projects target `net8.0-windows10.0.19041.0`
- [ ] All projects load successfully in Visual Studio 2022
- [ ] NuGet package restore succeeds for all projects
- [ ] KioskLibrary builds with 0 errors
- [ ] OrchestrationPollingManager builds with warnings (Phase 3 work noted)
- [ ] KioskClient builds with errors (Phase 4 XAML work pending) - **THIS IS EXPECTED**
- [ ] Test projects build successfully (may have test failures)
- [ ] Solution file updated with correct platform configurations
- [ ] All changes committed to feature branch

**Estimated Effort**: 20-32 hours

**Known Outstanding Issues** (to be addressed in later phases):
- ❌ KioskClient XAML compilation errors (Phase 4)
- ❌ Background task implementation stubbed out (Phase 3)
- ❌ AppCenter compatibility not verified (Phase 2)
- ❌ Some test failures expected (Phase 6)

---

## 6. Phase 2: Dependency Updates

**Duration**: 4-6 days  
**Complexity**: MEDIUM  
**Prerequisite**: Phase 1 complete (all projects using SDK-style, targeting .NET 8)

### Objectives
- Replace UWP-specific NuGet packages with Windows App SDK equivalents
- Verify/update third-party package compatibility
- Resolve package conflicts
- Update code to use new package APIs where needed
- Achieve successful build of KioskLibrary and OrchestrationPollingManager

### Package Migration Matrix

| Current Package (UWP) | Target Package (Windows App SDK) | Version | Migration Action | Risk |
|----------------------|----------------------------------|---------|------------------|------|
| **Microsoft.NETCore.UniversalWindowsPlatform** | **REMOVE** | N/A | Delete - no longer needed in .NET 8 | LOW |
| **Microsoft.UI.Xaml** (2.8.7) | **Microsoft.WindowsAppSDK** | 1.6.250106001 | Replace - includes WinUI 3 | MEDIUM |
| **CommunityToolkit.Uwp.Controls.SettingsControls** (8.2.250402) | **CommunityToolkit.WinUI.Controls.SettingsControls** | 8.1.240916 | Direct replacement | LOW |
| **Microsoft.AppCenter.\*** (5.0.7) | **VERIFY** | 5.0.7 or latest | Test compatibility or replace with Application Insights | HIGH |
| **Newtonsoft.Json** (13.0.4) | **KEEP** or migrate to System.Text.Json | 13.0.4 | Compatible - optionally modernize | LOW |
| **Serilog** (4.3.0) | **KEEP** | 4.3.0 | Fully compatible | LOW |
| **Serilog.Exceptions** (8.4.0) | **KEEP** | 8.4.0 | Fully compatible | LOW |
| **Serilog.Sinks.File** (7.0.0) | **KEEP** | 7.0.0 | Fully compatible | LOW |
| **Humanizer.Core** (2.14.1) | **KEEP** | 2.14.1 | Fully compatible | LOW |
| **System.Private.Uri** (4.3.2) | **REMOVE** | N/A | Included in .NET 8 BCL | LOW |
| **System.Text.RegularExpressions** (4.3.1) | **REMOVE** | N/A | Included in .NET 8 BCL | LOW |
| **System.IO** (4.3.0) | **REMOVE** | N/A | Included in .NET 8 BCL | LOW |
| **System.Xml.XDocument** (4.3.0) | **REMOVE** | N/A | Included in .NET 8 BCL | LOW |
| **System.Xml.XPath.XDocument** (4.3.0) | **REMOVE** | N/A | Included in .NET 8 BCL | LOW |

---

### Step 2.1: Update KioskLibrary Dependencies

**Current Dependencies**:
```xml
<PackageReference Include="Microsoft.NETCore.UniversalWindowsPlatform" Version="6.2.14" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<PackageReference Include="Serilog" Version="4.3.0" />
<PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
<PackageReference Include="System.Private.Uri" Version="4.3.2" />
<PackageReference Include="System.Text.RegularExpressions" Version="4.3.1" />
<PackageReference Include="System.Xml.XDocument" Version="4.3.0" />
<PackageReference Include="System.Xml.XPath.XDocument" Version="4.3.0" />
```

**Updated Dependencies** (already applied in Phase 1, verify):
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250106001" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<PackageReference Include="Serilog" Version="4.3.0" />
<PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
<!-- System.* packages removed - included in .NET 8 -->
```

**Code Updates Needed**:

1. **Update `using` Statements** (if any UWP-specific):
   - Review all `.cs` files in KioskLibrary
   - Replace `Windows.Foundation` with standard .NET types where applicable
   - Most code should remain unchanged

2. **Verify Storage APIs**:

   **File**: `Storage/ApplicationStorage.cs`

   Check compatibility of `Windows.Storage.ApplicationData`:
   ```csharp
   // This API is still available in Windows App SDK
   using Windows.Storage;

   public class ApplicationStorage : IApplicationStorage
   {
       public T GetSettingFromStorage<T>(string key)
       {
           var localSettings = ApplicationData.Current.LocalSettings;
           // ... existing code should work as-is
       }
   }
   ```

3. **Build and Test**:
   ```bash
   cd src/KioskLibrary
   dotnet clean
   dotnet restore
   dotnet build
   ```

   **Expected Result**: ✅ Build succeeds with 0 errors

4. **Commit Changes**:
   ```bash
   git add src/KioskLibrary/KioskLibrary.csproj
   git commit -m "Phase 2: Update KioskLibrary dependencies for Windows App SDK"
   ```

**Validation**:
- [ ] dotnet build succeeds with 0 errors
- [ ] No package conflict warnings
- [ ] Storage APIs still functional (will be tested in Phase 6)

---

### Step 2.2: Update OrchestrationPollingManager Dependencies

**Current Dependencies**:
```xml
<PackageReference Include="Microsoft.NETCore.UniversalWindowsPlatform" Version="6.2.14" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<PackageReference Include="Serilog" Version="4.3.0" />
<PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
<PackageReference Include="System.Private.Uri" Version="4.3.2" />
<PackageReference Include="System.Text.RegularExpressions" Version="4.3.1" />
```

**Updated Dependencies** (verify from Phase 1):
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250106001" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
<PackageReference Include="Serilog" Version="4.3.0" />
<PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
```

**Code Updates**:
- No code updates needed in this phase (background task redesign is Phase 3)
- Verify project still builds successfully

**Build and Test**:
```bash
cd src/OrchestrationPollingManager
dotnet clean
dotnet restore
dotnet build
```

**Expected Result**: ✅ Build succeeds (with `#warning` about Phase 3 work)

**Commit**:
```bash
git add src/OrchestrationPollingManager/OrchestrationPollingManager.csproj
git commit -m "Phase 2: Update OrchestrationPollingManager dependencies"
```

---

### Step 2.3: Update KioskClient Dependencies

**Current Dependencies** (from Phase 1):
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250106001" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1742" />
<PackageReference Include="CommunityToolkit.WinUI.Controls.SettingsControls" Version="8.1.240916" />
<PackageReference Include="Humanizer.Core" Version="2.14.1" />
<PackageReference Include="Serilog" Version="4.3.0" />
<PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageReference Include="Microsoft.AppCenter" Version="5.0.7" />
<PackageReference Include="Microsoft.AppCenter.Analytics" Version="5.0.7" />
<PackageReference Include="Microsoft.AppCenter.Crashes" Version="5.0.7" />
```

**Actions**:

1. **Verify AppCenter Compatibility**:

   **Research Task**: Check if Microsoft.AppCenter packages support Windows App SDK

   **Test Approach**:
   ```bash
   cd src/KioskClient
   dotnet build
   ```

   **Expected Scenarios**:

   ✅ **Scenario A: AppCenter is Compatible**
   - Build succeeds, no warnings
   - Keep existing packages
   - No code changes needed

   ⚠️ **Scenario B: AppCenter has Compatibility Warnings**
   - Build succeeds with warnings
   - Evaluate if warnings are acceptable
   - Consider upgrading to latest AppCenter version

   ❌ **Scenario C: AppCenter is Incompatible**
   - Build fails with AppCenter errors
   - **Action Required**: Migrate to Application Insights

2. **If AppCenter is Incompatible** (Scenario C), replace with Application Insights:

   **Remove AppCenter**:
   ```xml
   <!-- REMOVE these lines -->
   <PackageReference Include="Microsoft.AppCenter" Version="5.0.7" />
   <PackageReference Include="Microsoft.AppCenter.Analytics" Version="5.0.7" />
   <PackageReference Include="Microsoft.AppCenter.Crashes" Version="5.0.7" />
   ```

   **Add Application Insights**:
   ```xml
   <!-- ADD Application Insights -->
   <PackageReference Include="Microsoft.ApplicationInsights" Version="2.22.0" />
   <PackageReference Include="Microsoft.ApplicationInsights.WindowsApps" Version="2.22.0" />
   ```

   **Update App.xaml.cs**:
   ```csharp
   // REMOVE:
   // using Microsoft.AppCenter;
   // using Microsoft.AppCenter.Analytics;
   // using Microsoft.AppCenter.Crashes;
   // AppCenter.Start("a68e40e1-eead-431f-b091-34adecad9dbf", typeof(Analytics), typeof(Crashes));

   // ADD:
   using Microsoft.ApplicationInsights;
   using Microsoft.ApplicationInsights.Extensibility;

   public App()
   {
       this.InitializeComponent();
       this.Suspending += OnSuspending;

       // Initialize Application Insights
       TelemetryConfiguration.Active.InstrumentationKey = "YOUR_APP_INSIGHTS_KEY";
       var telemetryClient = new TelemetryClient();
       telemetryClient.TrackEvent("ApplicationStarted");
   }
   ```

   **⚠️ Note**: Requires provisioning Application Insights resource in Azure

3. **Verify CommunityToolkit.WinUI Package**:

   The package `CommunityToolkit.WinUI.Controls.SettingsControls` replaces the UWP version.

   **API Changes**: Minimal - most controls have same names/properties

   **Code Updates** (Phase 4):
   - Update XAML namespace: `using:CommunityToolkit.Uwp.UI.Controls` → `using:CommunityToolkit.WinUI.Controls`

4. **Build and Assess**:
   ```bash
   cd src/KioskClient
   dotnet clean
   dotnet restore
   dotnet build
   ```

   **Expected Result**: Build fails with XAML errors (Phase 4 work) - **THIS IS EXPECTED**

   **Key Check**: No package conflict errors or package incompatibility errors

**Commit**:
```bash
git add src/KioskClient/KioskClient.csproj
git add src/KioskClient/App.xaml.cs  # if AppCenter was replaced
git commit -m "Phase 2: Update KioskClient dependencies (AppCenter verified/replaced)"
```

---

### Step 2.4: Optional - Migrate Newtonsoft.Json to System.Text.Json

**Rationale**:
- `System.Text.Json` is built into .NET 8 (better performance, no external dependency)
- Modern, actively developed by Microsoft
- May have breaking API differences

**Risk Assessment**: MEDIUM (API differences may require code changes)

**Decision**: **DEFER to post-migration optimization** (not critical for migration success)

**If Pursuing**:
1. Remove `Newtonsoft.Json` package reference
2. Update `using Newtonsoft.Json;` → `using System.Text.Json;`
3. Update serialization calls:
   ```csharp
   // OLD:
   var json = JsonConvert.SerializeObject(obj);
   var obj = JsonConvert.DeserializeObject<T>(json);

   // NEW:
   var json = JsonSerializer.Serialize(obj);
   var obj = JsonSerializer.Deserialize<T>(json);
   ```
4. Test thoroughly - serialization behavior may differ

**Recommendation**: ✅ **Keep Newtonsoft.Json for now**, migrate later if needed

---

### Step 2.5: Update Test Project Dependencies

**For Each Test Project**:

1. **CommonTestLibrary** - No changes needed (uses standard test packages)

2. **KioskLibrary.Spec**:
   - Verify project reference to KioskLibrary works
   - No package changes needed

3. **OrchestrationPollingManager.Spec**:
   - Verify project references work
   - No package changes needed

4. **KioskClient.Spec**:
   - May need UI testing packages
   - Add WinUI 3 testing support:
     ```xml
     <PackageReference Include="Microsoft.Testing.Extensions.WinUI" Version="1.0.0" />
     ```

**Build All Test Projects**:
```bash
dotnet build test/Common/CommonTestLibrary/CommonTestLibrary.csproj
dotnet build test/Unit/KioskLibrary.Spec/KioskLibrary.Spec.csproj
dotnet build test/Unit/OrchestrationPollingManager.Spec/OrchestrationPollingManager.Spec.csproj
dotnet build test/Unit/KioskClient.Spec/KioskClient.Spec.csproj
```

**Expected**: All test projects build successfully

**Commit**:
```bash
git add test/
git commit -m "Phase 2: Update test project dependencies"
```

---

### Step 2.6: Resolve Package Conflicts

**Check for Conflicts**:
```bash
dotnet list package --include-transitive
```

**Common Conflict Scenarios**:

1. **Multiple versions of System.* packages**:
   - Usually auto-resolved by .NET SDK
   - If errors occur, add explicit `<PackageReference>` with specific version

2. **Windows SDK version mismatches**:
   - Ensure all projects use same `TargetFramework` (`net8.0-windows10.0.19041.0`)
   - Verify `Microsoft.WindowsAppSDK` version is consistent

3. **Runtime conflicts**:
   - Check for incompatible runtime identifiers
   - Ensure `<RuntimeIdentifiers>` are consistent

**Resolution Steps**:
1. Identify conflicting package (from build error message)
2. Add explicit `<PackageReference>` in affected project
3. Rebuild and verify conflict resolved

**Commit Conflict Resolutions**:
```bash
git add *.csproj
git commit -m "Phase 2: Resolve package version conflicts"
```

---

### Validation Checklist (Phase 2 Complete When)

- [ ] All UWP-specific packages removed
- [ ] Windows App SDK packages added to all projects
- [ ] `Microsoft.WindowsAppSDK` version consistent across projects (1.6.250106001)
- [ ] CommunityToolkit.WinUI packages replace CommunityToolkit.Uwp
- [ ] AppCenter compatibility verified OR replaced with Application Insights
- [ ] No package conflict errors
- [ ] KioskLibrary builds with 0 errors ✅
- [ ] OrchestrationPollingManager builds with 0 errors ✅ (expected warning about Phase 3)
- [ ] KioskClient fails to build due to XAML errors ⚠️ (expected - Phase 4 work)
- [ ] All test projects build successfully ✅
- [ ] `dotnet list package` shows no security vulnerabilities
- [ ] All changes committed to feature branch

**Estimated Effort**: 16-24 hours

**Known Outstanding Issues** (to be addressed in later phases):
- ❌ KioskClient XAML compilation errors (Phase 4)
- ❌ Background task implementation (Phase 3)
- ⚠️ Some test failures may occur (Phase 6)

---

## 7. Phase 3: Background Task Re-Architecture

**Duration**: 3-5 days  
**Complexity**: HIGH  
**Prerequisite**: Phase 2 complete (dependencies updated)

### Objectives
- Redesign UWP `IBackgroundTask` implementation for Windows App SDK
- Maintain orchestration polling functionality
- Ensure background updates work in kiosk mode
- Validate background task registration and execution
- Achieve working end-to-end orchestration update flow

### Background Task Challenge

**UWP Implementation** (Current):
```csharp
public sealed class OrchestrationUpdateTask : IBackgroundTask
{
    public async void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();
        await Orchestrator.GetNextOrchestration(new HttpHelper(), new ApplicationStorage());
        deferral.Complete();
    }
}
```

- Uses `Windows.ApplicationModel.Background.IBackgroundTask`
- Registered via `BackgroundTaskBuilder` with `TimeTrigger`
- Declared in Package.appxmanifest under `<Extensions>`

**Windows App SDK Challenge**:
- ❌ `IBackgroundTask` interface **NOT available** in Windows App SDK
- ❌ Traditional UWP background tasks **deprecated**
- ✅ **Alternative approaches** available

---

### Step 3.1: Research Windows App SDK Background Task Options

**Option A: App Services (Recommended for Kiosk Scenarios)**
- Uses Windows App SDK App Service extension
- In-process or out-of-process execution
- Good for periodic tasks
- **Best for kiosk mode** (stays within app package)

**Option B: Windows Service**
- Traditional Windows Service (runs as system service)
- Requires admin privileges to install
- More complex deployment
- **Not ideal for Microsoft Store apps**

**Option C: Scheduled Tasks (Task Scheduler)**
- Use Windows Task Scheduler API
- Simple periodic execution
- Limited integration with app lifecycle
- **Suitable for simple polling scenarios**

**Option D: In-App Timer (Simplest)**
- Application-managed background thread
- Works only when app is running
- **Suitable if kiosk app never closes**

**Recommended Approach**: **Option D (In-App Timer)** for kiosk scenario

**Rationale**:
- ✅ Kiosk apps run continuously (never close)
- ✅ Simplest implementation
- ✅ No complex registration needed
- ✅ Maintained within app lifecycle
- ✅ Easy to debug and test
- ✅ No Store certification complications

---

### Step 3.2: Implement In-App Background Polling Service

**Create New Class**: `Services/OrchestrationPollingService.cs` in KioskClient project

```csharp
// src/KioskClient/Services/OrchestrationPollingService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using KioskLibrary.Helpers;
using KioskLibrary.Orchestrations;
using KioskLibrary.Storage;
using KioskLibrary.Common;
using Serilog;

namespace KioskClient.Services
{
    /// <summary>
    /// Background service for polling orchestration updates
    /// Replaces UWP IBackgroundTask implementation
    /// </summary>
    public class OrchestrationPollingService : IDisposable
    {
        private readonly IHttpHelper _httpHelper;
        private readonly IApplicationStorage _storage;
        private Timer _pollingTimer;
        private bool _isDisposed;

        public OrchestrationPollingService(IHttpHelper httpHelper, IApplicationStorage storage)
        {
            _httpHelper = httpHelper ?? throw new ArgumentNullException(nameof(httpHelper));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <summary>
        /// Starts the polling service
        /// </summary>
        public void Start()
        {
            Log.Information("OrchestrationPollingService starting");

            var pollingInterval = _storage.GetSettingFromStorage<int>(Constants.ApplicationStorage.Settings.PollingInterval);

            if (pollingInterval > 0)
            {
                // Convert minutes to milliseconds
                var intervalMs = pollingInterval * 60 * 1000;

                // Create timer: first run after initial delay, then repeat at interval
                _pollingTimer = new Timer(
                    callback: async _ => await PollOrchestrationAsync(),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(1), // First poll after 1 minute
                    period: TimeSpan.FromMilliseconds(intervalMs)
                );

                Log.Information("OrchestrationPollingService started with {Interval} minute interval", pollingInterval);
            }
            else
            {
                Log.Warning("OrchestrationPollingService not started - polling interval is 0 or not set");
            }
        }

        /// <summary>
        /// Stops the polling service
        /// </summary>
        public void Stop()
        {
            Log.Information("OrchestrationPollingService stopping");
            _pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _pollingTimer?.Dispose();
            _pollingTimer = null;
        }

        /// <summary>
        /// Polls the orchestration endpoint for updates
        /// </summary>
        private async Task PollOrchestrationAsync()
        {
            try
            {
                Log.Information("OrchestrationPollingService executing poll");
                await Orchestrator.GetNextOrchestration(_httpHelper, _storage);
                Log.Information("OrchestrationPollingService poll completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OrchestrationPollingService poll failed");
                // Don't throw - let timer continue running
            }
        }

        /// <summary>
        /// Manually triggers an immediate poll (for testing/debugging)
        /// </summary>
        public async Task TriggerManualPollAsync()
        {
            Log.Information("Manual orchestration poll triggered");
            await PollOrchestrationAsync();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _isDisposed = true;
            }
        }
    }
}
```

**Add to KioskClient.csproj**:
```xml
<ItemGroup>
  <Compile Include="Services\OrchestrationPollingService.cs" />
</ItemGroup>
```

---

### Step 3.3: Integrate Polling Service into Application Lifecycle

**Update App.xaml.cs** to start/stop polling service:

```csharp
// src/KioskClient/App.xaml.cs
using KioskClient.Services;
using KioskLibrary.Helpers;
using KioskLibrary.Storage;
using Microsoft.UI.Xaml;
using Serilog;

namespace KioskClient
{
    public partial class App : Application
    {
        private OrchestrationPollingService _pollingService;

        public App()
        {
            this.InitializeComponent();

            // Initialize polling service
            _pollingService = new OrchestrationPollingService(
                new HttpHelper(),
                new ApplicationStorage()
            );
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // ... existing window activation code ...

            // Start background polling
            _pollingService.Start();
            Log.Information("Application launched - polling service started");
        }

        // Handle application exit (if needed in kiosk mode)
        ~App()
        {
            _pollingService?.Dispose();
        }
    }
}
```

---

### Step 3.4: Remove UWP Background Task Infrastructure

**Delete Obsolete Code**:

1. **Delete** `src/OrchestrationPollingManager/OrchestrationUpdateTask.cs` (entire file)

2. **Keep** `OrchestrationPollingManager` project for potential shared logic, OR

3. **Migrate shared logic** to `KioskLibrary` and delete `OrchestrationPollingManager` project entirely

**Recommended**: **Option 3 - Consolidate into KioskLibrary**

**Actions**:
- OrchestrationPollingManager contained only the background task
- All actual logic is in `KioskLibrary.Orchestrations.Orchestrator`
- Safe to **delete OrchestrationPollingManager project**

**Steps**:
```bash
# Remove project from solution
# In Visual Studio: Right-click OrchestrationPollingManager → Remove

# Delete project folder
Remove-Item -Recurse -Force src/OrchestrationPollingManager
Remove-Item -Recurse -Force test/Unit/OrchestrationPollingManager.Spec
```

**Update KioskClient.csproj** - Remove ProjectReference:
```xml
<!-- REMOVE this line -->
<ProjectReference Include="..\OrchestrationPollingManager\OrchestrationPollingManager.csproj" />
```

**Update Solution File**:
- Remove OrchestrationPollingManager and OrchestrationPollingManager.Spec from KioskClient.sln

---

### Step 3.5: Update Package.appxmanifest (Remove Background Task Extension)

**Current Manifest** (Package.appxmanifest):
```xml
<Extensions>
  <Extension Category="windows.backgroundTasks" 
             EntryPoint="OrchestrationPollingManager.OrchestrationUpdateTask">
    <BackgroundTasks>
      <Task Type="timer"/>
    </BackgroundTasks>
  </Extension>
</Extensions>
```

**Updated Manifest** (Windows App SDK format - detailed in Phase 5):
```xml
<!-- REMOVE entire <Extensions> block related to background tasks -->
<!-- No background task extension needed for in-app timer approach -->
```

**Note**: Full manifest migration happens in Phase 5, but remove background task extension now.

---

### Step 3.6: Add Manual Poll Trigger for Testing

**Add UI Button** (optional, for debugging):

**In MainPage.xaml** (temporary test button):
```xml
<Button Content="Test Poll Orchestration" 
        Click="TestPollButton_Click"
        Visibility="Collapsed" /> <!-- Hidden in production, show for testing -->
```

**In MainPage.xaml.cs**:
```csharp
private async void TestPollButton_Click(object sender, RoutedEventArgs e)
{
    var app = (App)Application.Current;
    await app.PollingService.TriggerManualPollAsync();

    // Show confirmation
    var dialog = new ContentDialog
    {
        Title = "Poll Triggered",
        Content = "Orchestration poll executed. Check logs for results.",
        CloseButtonText = "OK",
        XamlRoot = this.Content.XamlRoot
    };
    await dialog.ShowAsync();
}
```

**Expose PollingService** in App.xaml.cs:
```csharp
public class App : Application
{
    public OrchestrationPollingService PollingService => _pollingService;
    // ... rest of code ...
}
```

---

### Step 3.7: Test Background Polling

**Test Plan**:

1. **Set Polling Interval** in app settings (via UI or storage):
   ```csharp
   var storage = new ApplicationStorage();
   storage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.PollingInterval, 5); // 5 minutes
   ```

2. **Launch Application**:
   ```bash
   dotnet run --project src/KioskClient/KioskClient.csproj
   ```

3. **Verify Polling Starts**:
   - Check Serilog output for "OrchestrationPollingService started" message
   - Verify timer is created

4. **Wait for First Poll** (1 minute after start):
   - Check logs for "OrchestrationPollingService executing poll"
   - Verify orchestration data retrieved

5. **Verify Interval** (wait 5 minutes):
   - Check logs for repeated poll executions
   - Verify interval matches configured value

6. **Test Manual Poll** (if test button added):
   - Click test button
   - Verify immediate poll execution

7. **Test Error Handling**:
   - Disconnect network
   - Wait for poll execution
   - Verify error logged but timer continues

**Expected Results**:
- ✅ Polling starts automatically on app launch
- ✅ Polls execute at configured interval
- ✅ Errors don't crash the app
- ✅ Orchestration updates apply correctly

---

### Alternative Approach: If Kiosk App CAN Close

**If app doesn't run continuously**, use **App Service** approach:

**Quick Overview** (detailed implementation if needed):

1. Create separate "Background Service" project
2. Implement `IBackgroundTask` equivalent for Windows App SDK
3. Register as App Service extension in manifest
4. More complex, requires:
   - Inter-process communication
   - Service lifecycle management
   - Additional testing

**Recommendation**: Only pursue if in-app timer insufficient.

---

### Validation Checklist (Phase 3 Complete When)

- [ ] `OrchestrationPollingService.cs` created in KioskClient project
- [ ] Service integrated into `App.xaml.cs` lifecycle
- [ ] OrchestrationPollingManager project deleted (if consolidating)
- [ ] Background task extension removed from manifest
- [ ] Test button added for manual poll testing (optional)
- [ ] Application builds successfully
- [ ] Polling service starts on app launch (verified in logs)
- [ ] First poll executes after initial delay (verified in logs)
- [ ] Subsequent polls execute at configured interval (verified in logs)
- [ ] Manual poll trigger works (if implemented)
- [ ] Error handling tested (network disconnection)
- [ ] Orchestration updates still apply correctly
- [ ] All changes committed to feature branch

**Estimated Effort**: 12-20 hours

**Critical Success Factors**:
- ✅ Polling functionality preserved
- ✅ No regression in orchestration update behavior
- ✅ Works reliably in kiosk mode (continuous operation)
- ✅ Error handling prevents app crashes

**Known Outstanding Issues** (to be addressed in later phases):
- ❌ KioskClient XAML compilation errors (Phase 4)
- ⚠️ Manifest updates pending (Phase 5)

---

## 8. Phase 4: UI Migration (XAML & Code-Behind)

**Duration**: 4-6 days  
**Complexity**: MEDIUM-HIGH  
**Prerequisite**: Phase 3 complete (background polling working)

### Objectives
- Update all XAML files from UWP to WinUI 3 syntax
- Migrate namespace references (Windows.UI.Xaml → Microsoft.UI.Xaml)
- Update code-behind files for WinUI 3 APIs
- Fix window management and navigation
- Achieve successful build and app launch

### XAML Namespace Migration Matrix

| UWP Namespace | WinUI 3 Namespace | Usage |
|---------------|-------------------|-------|
| `Windows.UI.Xaml` | `Microsoft.UI.Xaml` | Core XAML types |
| `Windows.UI.Xaml.Controls` | `Microsoft.UI.Xaml.Controls` | UI controls |
| `Windows.UI.Xaml.Navigation` | `Microsoft.UI.Xaml.Navigation` | Navigation |
| `Windows.UI.Xaml.Media` | `Microsoft.UI.Xaml.Media` | Brushes, transforms |
| `Windows.UI` | `Microsoft.UI` | Colors, general UI |
| `using:CommunityToolkit.Uwp.UI.Controls` | `using:CommunityToolkit.WinUI.Controls` | Community Toolkit |

---

### Step 4.1: Update App.xaml

**Current** (`App.xaml`):
```xml
<Application
    x:Class="KioskClient.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <!-- Resources -->
    </Application.Resources>
</Application>
```

**Updated** (WinUI 3):
```xml
<Application
    x:Class="KioskClient.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <!-- Existing resource dictionaries -->
                <ResourceDictionary Source="Resources/GreenControls.xaml"/>
                <ResourceDictionary Source="Resources/KioskClientLogo.xaml"/>
                <!-- ... other resources ... -->
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Update App.xaml.cs**:
```csharp
using Microsoft.UI.Xaml;  // Changed from Windows.UI.Xaml
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
// ... other usings ...

namespace KioskClient
{
    public partial class App : Application
    {
        private Window m_window;  // WinUI 3 uses explicit Window reference

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new Window();
            Frame rootFrame = new Frame();

            m_window.Content = rootFrame;
            rootFrame.Navigate(typeof(MainPage), args.Arguments);

            m_window.Activate();

            // Start polling service
            _pollingService.Start();
        }
    }
}
```

---

### Step 4.2: Update All XAML Page Files

**For Each XAML File** (MainPage.xaml, Settings.xaml, About.xaml, etc.):

**Find/Replace Operations**:

1. **Namespace xmlns declarations** (at top of each file):
   ```xml
   <!-- No changes needed - default xmlns still works -->
   ```

2. **using: namespace references** (in XAML):
   ```xml
   <!-- OLD -->
   xmlns:controls="using:CommunityToolkit.Uwp.UI.Controls"

   <!-- NEW -->
   xmlns:controls="using:CommunityToolkit.WinUI.Controls"
   ```

3. **Code-behind using statements**:
   ```csharp
   // OLD:
   using Windows.UI.Xaml;
   using Windows.UI.Xaml.Controls;
   using Windows.UI.Xaml.Navigation;

   // NEW:
   using Microsoft.UI.Xaml;
   using Microsoft.UI.Xaml.Controls;
   using Microsoft.UI.Xaml.Navigation;
   ```

**Automated Migration** (PowerShell script):

```powershell
# Run from repository root
$files = Get-ChildItem -Path "src/KioskClient" -Include "*.xaml.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'using Windows\.UI\.Xaml', 'using Microsoft.UI.Xaml'
    $content = $content -replace 'using Windows\.UI\.', 'using Microsoft.UI.'
    Set-Content -Path $file.FullName -Value $content
}

Write-Host "Namespace migration complete for code-behind files"
```

**Manual XAML Updates**:
- Update `xmlns:` declarations in each XAML file
- Update `using:CommunityToolkit.Uwp` → `using:CommunityToolkit.WinUI`

---

### Step 4.3: Update Resource Dictionaries

**Files to Update**:
- `Resources/GreenControls.xaml`
- `Resources/KioskClientLogo.xaml`
- `Resources/InvertedKioskClientLogo.xaml`
- `Resources/BadgeLogo.xaml`
- `Resources/CityOfStantonTechnology.xaml`

**Common Changes**:
- No xmlns changes needed (default namespace still works)
- Update any control-specific syntax if errors occur

---

### Step 4.4: Update Navigation & Window Management

**MainPage.xaml.cs** - Update navigation:

```csharp
// OLD (UWP):
Frame.Navigate(typeof(SettingsPage), args);

// NEW (WinUI 3):
Frame.Navigate(typeof(SettingsPage), args);  // Same API! ✅
```

**Window Management**:
```csharp
// OLD (UWP):
Window.Current.Activate();

// NEW (WinUI 3):
// Window reference must be passed or accessed differently
// Use window from App.xaml.cs
var window = (Application.Current as App).m_window;
window.Activate();
```

**ContentDialog Updates**:
```csharp
// OLD (UWP):
var dialog = new ContentDialog { ... };
await dialog.ShowAsync();

// NEW (WinUI 3):
var dialog = new ContentDialog 
{ 
    XamlRoot = this.Content.XamlRoot,  // REQUIRED in WinUI 3
    ...
};
await dialog.ShowAsync();
```

---

### Step 4.5: Update Converters

**Files**:
- `Converters/BooleanToVisibilityConverter.cs`
- `Converters/BooleanToColorConverter.cs`
- etc.

**Changes**:
```csharp
// Update using statements
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

// IValueConverter interface unchanged - no code changes needed
```

---

### Step 4.6: Build and Fix Compilation Errors

**Build Application**:
```bash
cd src/KioskClient
dotnet build
```

**Common Errors & Fixes**:

1. **Error**: `'Window' does not contain a definition for 'Current'`
   - **Fix**: Use explicit window reference from App

2. **Error**: `ContentDialog must have XamlRoot set`
   - **Fix**: Add `XamlRoot = this.Content.XamlRoot` to all ContentDialog instances

3. **Error**: `Type 'SomeControl' not found`
   - **Fix**: Verify namespace updated correctly, check package reference

4. **Error**: Color/Brush syntax errors
   - **Fix**: Update to WinUI 3 syntax if needed

**Iteratively Fix Errors**:
- Address errors one file at a time
- Test compilation after each fix
- Commit working changes incrementally

---

### Step 4.7: Test Application Launch

**Run Application**:
```bash
dotnet run --project src/KioskClient/KioskClient.csproj
```

**Validation**:
- [ ] Application launches without crash
- [ ] Main window displays
- [ ] MainPage renders correctly
- [ ] Navigation works (navigate to Settings, About, etc.)
- [ ] All pages display without errors
- [ ] Dialogs show correctly (with XamlRoot set)

---

### Validation Checklist (Phase 4 Complete When)

- [ ] All XAML files updated with WinUI 3 namespaces
- [ ] All code-behind files updated (using Microsoft.UI.*)
- [ ] Resource dictionaries updated
- [ ] Converters updated
- [ ] Window management fixed
- [ ] ContentDialog XamlRoot set in all instances
- [ ] Application builds with 0 errors ✅
- [ ] Application launches successfully ✅
- [ ] MainPage displays correctly ✅
- [ ] All pages navigate without errors ✅
- [ ] Dialogs show correctly ✅
- [ ] No visual regressions observed
- [ ] All changes committed

**Estimated Effort**: 16-24 hours

---

## 9. Phase 5: Packaging & Manifest Updates

**Duration**: 2-3 days  
**Complexity**: MEDIUM  
**Prerequisite**: Phase 4 complete (app launches successfully)

### Objectives
- Update Package.appxmanifest to Windows App SDK schema
- Preserve Store identity and kiosk mode extensions
- Generate valid MSIX package
- Test package installation and kiosk mode configuration

### Step 5.1: Update Package.appxmanifest

**Current Manifest** (UWP format):
```xml
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" ...>
  <Identity Name="5999BirchCom.385071C3770F8" Publisher="CN=..." Version="1.2.0.0" />
  <!-- ... -->
</Package>
```

**Updated Manifest** (Windows App SDK format):
```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap mp rescap">

  <!-- Preserve Store identity -->
  <Identity
    Name="5999BirchCom.385071C3770F8"
    Publisher="CN=291573D4-2B42-451F-9AD2-2CCDBBAF6DB9"
    Version="2.0.0.0" />  <!-- Increment version for Windows App SDK release -->

  <Properties>
    <DisplayName>Kiosk Client</DisplayName>
    <PublisherDisplayName>City of Stanton</PublisherDisplayName>
    <Logo>Assets\AppIcon\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="x-generate"/>
  </Resources>

  <Applications>
    <Application Id="App" Executable="$targetnametoken$.exe" EntryPoint="$targetentrypoint$">
      <uap:VisualElements
        DisplayName="Kiosk Client"
        Description="A modern app for Windows kiosk mode operations."
        BackgroundColor="#ffffff"
        Square150x150Logo="Assets\AppIcon\Square150x150Logo.png"
        Square44x44Logo="Assets\AppIcon\Square44x44Logo.png">
        <uap:DefaultTile ShortName="Kiosk Client" Square71x71Logo="Assets\AppIcon\SmallTile.png" Wide310x150Logo="Assets\AppIcon\Wide310x150Logo.png" Square310x310Logo="Assets\AppIcon\LargeTile.png">
          <uap:ShowNameOnTiles>
            <uap:ShowOn Tile="square310x310Logo"/>
            <uap:ShowOn Tile="square150x150Logo"/>
            <uap:ShowOn Tile="wide310x150Logo"/>
          </uap:ShowNameOnTiles>
        </uap:DefaultTile>
        <uap:SplashScreen BackgroundColor="#277C31" Image="Assets\AppIcon\SplashScreen.png"/>
        <uap:LockScreen Notification="badge" BadgeLogo="Assets\AppIcon\BadgeLogo.png"/>
      </uap:VisualElements>
      <!-- Background task extension removed - using in-app polling -->
    </Application>
  </Applications>

  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />  <!-- Required for Windows App SDK -->
  </Capabilities>
</Package>
```

**Key Changes**:
- ✅ Updated `TargetDeviceFamily` to `Windows.Desktop`
- ✅ Added `rescap:Capability` for `runFullTrust`
- ✅ Removed background task extension (now using in-app timer)
- ✅ Preserved Store identity
- ✅ Incremented version to 2.0.0.0

### Step 5.2: Generate MSIX Package

**Build MSIX**:
```bash
cd src/KioskClient
dotnet publish -c Release -r win-x64 --self-contained
```

**Create Package**:
- Visual Studio: Project → Publish → Create App Packages
- Select: Microsoft Store (using existing app name)
- Configuration: Release
- Architectures: x86, x64, ARM64
- Generate bundle: Always

**Output**: `.msixupload` file for Store submission

### Step 5.3: Validate with WACK

**Run Windows App Certification Kit**:
```powershell
# Install WACK if not available (comes with Windows SDK)
# Run certification
& "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe" test -apptype WindowsAppSDK -packagepath "path\to\package.msix"
```

**Fix Any Errors** reported by WACK before Store submission.

### Validation Checklist (Phase 5 Complete)

- [ ] Package.appxmanifest updated to Windows App SDK schema
- [ ] Store identity preserved
- [ ] Kiosk mode capabilities maintained
- [ ] MSIX package generates successfully
- [ ] Package installs on test device
- [ ] WACK validation passes
- [ ] All changes committed

**Estimated Effort**: 8-12 hours

---

## 10. Phase 6: Testing & Validation

**Duration**: 5-7 days  
**Complexity**: MEDIUM  
**Prerequisite**: Phase 5 complete (MSIX package builds)

### Step 6.1: Unit Test Execution

**Run All Test Projects**:
```bash
dotnet test test/Unit/KioskLibrary.Spec/KioskLibrary.Spec.csproj
dotnet test test/Unit/KioskClient.Spec/KioskClient.Spec.csproj
```

**Fix Failing Tests**:
- Update test mocks for Windows App SDK APIs
- Fix assertions broken by namespace changes
- Update test data if needed

### Step 6.2: Functional Testing

**Test Scenarios**:
- [ ] Application launches
- [ ] MainPage displays correctly
- [ ] Navigate to Settings page
- [ ] Navigate to About dialog
- [ ] Examples dialog shows correctly
- [ ] Image action displays
- [ ] Website action displays
- [ ] Orchestration polling works (check logs)
- [ ] Settings persist correctly
- [ ] Logging to file works

### Step 6.3: Kiosk Mode Validation

**Configure Assigned Access**:
1. Install MSIX on Windows 11 test device
2. Settings → Accounts → Other users → Set up a kiosk
3. Select "Kiosk Client" app
4. Configure kiosk account
5. Sign in as kiosk user

**Verify**:
- [ ] App launches in full-screen kiosk mode
- [ ] Cannot exit to Windows shell
- [ ] App functions correctly in kiosk mode
- [ ] Background polling continues
- [ ] Device can be managed remotely (if configured)

### Step 6.4: Performance Testing

**Baseline Metrics**:
- Startup time
- Memory usage
- CPU usage during polling

**Compare**: Windows App SDK vs original UWP (should be equal or better)

### Validation Checklist (Phase 6 Complete)

- [ ] All unit tests pass
- [ ] All functional test scenarios pass
- [ ] Kiosk mode configuration successful
- [ ] App runs correctly in kiosk mode
- [ ] Performance acceptable
- [ ] No crashes or errors in logs

**Estimated Effort**: 20-28 hours

---

## 11. Phase 7: Store Submission & Deployment

**Duration**: 2-3 days  
**Complexity**: LOW  
**Prerequisite**: Phase 6 complete (all tests pass)

### Step 7.1: Prepare Store Submission

**Update Partner Center**:
- App listing details (mention Windows App SDK)
- Screenshots (if UI changed)
- What's new: "Migrated to Windows App SDK for improved performance"

### Step 7.2: Submit Package

**Upload**:
- Upload `.msixupload` file
- Submit for certification
- Monitor certification status

### Step 7.3: Release

**Staged Rollout** (recommended):
- Release to 10% of users initially
- Monitor crash reports
- Expand to 100% after verification

### Validation Checklist (Phase 7 Complete)

- [ ] Store listing updated
- [ ] Package submitted
- [ ] Certification passed
- [ ] App published to Store
- [ ] Users can download/install successfully

**Estimated Effort**: 8-12 hours

---

## 12. Risk Management

### High-Risk Items

| Risk | Probability | Impact | Mitigation | Contingency |
|------|-------------|--------|------------|-------------|
| **Background task incompatibility** | Medium | High | In-app timer approach (Phase 3) | Fallback to Windows Service if needed |
| **AppCenter SDK incompatible** | Medium | Medium | Verify early (Phase 2), prepare Application Insights alternative | Replace with Application Insights |
| **XAML migration breaks UI** | Low | High | Incremental testing, visual comparison | Revert specific XAML files if needed |
| **Kiosk mode regression** | Low | Critical | Dedicated kiosk testing (Phase 6) | Retain UWP version as fallback |
| **Store certification failure** | Low | Medium | WACK validation before submission | Address certification issues iteratively |
| **Performance degradation** | Low | Medium | Performance baseline + comparison | Optimize or investigate root cause |

### Risk Response Plans

**If Background Polling Fails**:
1. Verify timer is created and firing
2. Check logs for errors
3. Test manual poll trigger
4. Fallback: Implement Windows Service approach

**If Kiosk Mode Doesn't Work**:
1. Verify manifest capabilities
2. Check Assigned Access configuration
3. Test on different Windows 11 version
4. Escalate to Microsoft support if blocked

---

## 13. Testing & Validation Strategy

### Multi-Level Testing

#### Level 1: Unit Tests
- **Scope**: KioskLibrary, domain logic
- **Framework**: xUnit
- **Coverage Goal**: >80% for business logic
- **Timing**: After Phase 2 (dependency updates)

#### Level 2: Integration Tests
- **Scope**: API integration, storage, orchestration
- **Coverage**: HTTP polling, JSON parsing, storage operations
- **Timing**: After Phase 3 (background task complete)

#### Level 3: UI Tests
- **Scope**: Page navigation, dialogs, visual elements
- **Manual Testing**: Required for XAML changes
- **Timing**: After Phase 4 (UI migration complete)

#### Level 4: System Tests
- **Scope**: End-to-end scenarios, kiosk mode
- **Environment**: Test device with Assigned Access
- **Timing**: After Phase 5 (packaging complete)

#### Level 5: Acceptance Tests
- **Scope**: User acceptance, performance
- **Participants**: Stakeholders, IT team
- **Timing**: After Phase 6 (all tests pass)

---

## 14. Complexity & Effort Assessment

### Per-Project Complexity

| Project | Complexity | Effort (hours) | Justification |
|---------|------------|----------------|---------------|
| **KioskLibrary** | MEDIUM | 8-12 | SDK-style conversion, dependency updates, storage API verification |
| **OrchestrationPollingManager** | HIGH | 16-24 | Complete background task redesign |
| **KioskClient** | MEDIUM-HIGH | 20-30 | Extensive XAML updates, window management, manifest updates |
| **Test Projects** | LOW-MEDIUM | 13-21 | Framework updates, mock updates, test fixes |
| **Integration & Testing** | MEDIUM | 20-28 | Functional, kiosk, performance testing |
| **Packaging & Deployment** | LOW | 8-12 | MSIX generation, WACK validation, Store submission |

**Total Estimated Effort**: **85-127 hours** (2-3 developer-weeks)

### Effort Distribution by Phase

| Phase | Duration | Effort (hours) | % of Total |
|-------|----------|----------------|------------|
| Phase 0: Preparation | 3-5 days | 16-20 | 15% |
| Phase 1: Project Structure | 5-8 days | 20-32 | 25% |
| Phase 2: Dependencies | 4-6 days | 16-24 | 19% |
| Phase 3: Background Tasks | 3-5 days | 12-20 | 15% |
| Phase 4: UI Migration | 4-6 days | 16-24 | 19% |
| Phase 5: Packaging | 2-3 days | 8-12 | 9% |
| Phase 6: Testing | 5-7 days | 20-28 | 22% |
| Phase 7: Deployment | 2-3 days | 8-12 | 9% |

**Total Timeline**: **3-4 weeks** (with buffer for unexpected issues)

---

## 15. Source Control Strategy

### Branching Strategy

**Feature Branch**:
```
feature/windows-app-sdk-migration
```

**Commit Strategy**:
- **Atomic commits**: One logical change per commit
- **Commit per phase step**: Enables easy rollback
- **Descriptive messages**: "Phase X: [action]"

**Example Commit Messages**:
```
Phase 1.1: Migrate KioskLibrary to .NET 8 SDK-style project
Phase 2.1: Update KioskLibrary dependencies for Windows App SDK
Phase 3.2: Implement OrchestrationPollingService for in-app background polling
Phase 4.3: Update MainPage.xaml to WinUI 3 namespaces
```

### Pull Request Strategy

**Create PR After**:
- Phase 1 complete (project structure modernized)
- Phase 3 complete (background task working)
- Phase 4 complete (UI functional)
- Phase 6 complete (all tests pass)

**PR Requirements**:
- [ ] All builds succeed
- [ ] All tests pass
- [ ] Code review by 1+ team member
- [ ] WACK validation passed (for final PR)

**Merge Strategy**:
- Squash merge for cleaner history (optional)
- OR preserve commit history for traceability

### Backup & Rollback

**Tags**:
```bash
git tag v1.2.0-uwp-final       # Before migration starts
git tag v2.0.0-winappsd-phase1 # After Phase 1
git tag v2.0.0-winappsd-phase4 # After Phase 4 (UI working)
git tag v2.0.0-release         # Final Windows App SDK release
```

**Rollback Procedure**:
```bash
# If Phase X fails critically
git revert <commit-hash>
# OR
git reset --hard v2.0.0-winappsd-phaseY  # Revert to previous phase
```

---

## 16. Success Criteria

### Technical Criteria

#### Build & Compilation
- [x] All projects load in Visual Studio 2022 without errors
- [x] All projects target .NET 8.0 (`net8.0-windows10.0.19041.0`)
- [x] NuGet package restore succeeds for all projects
- [x] Solution builds with 0 errors
- [x] All compiler warnings addressed or documented

#### Functionality
- [x] Application launches successfully
- [x] All pages navigate without errors (MainPage, Settings, About, etc.)
- [x] Dialogs display correctly
- [x] Background orchestration polling works
- [x] Orchestration updates apply correctly
- [x] Application settings persist
- [x] Logging to file works

#### Testing
- [x] All unit tests pass (KioskLibrary.Spec, etc.)
- [x] Integration tests pass
- [x] Manual functional testing complete
- [x] No regressions in existing functionality

#### Kiosk Mode
- [x] MSIX package installs successfully
- [x] Assigned Access (kiosk mode) can be configured
- [x] Application runs in full-screen kiosk mode
- [x] Cannot exit to Windows shell in kiosk mode
- [x] Background polling continues in kiosk mode
- [x] All features work correctly in kiosk mode

#### Microsoft Store
- [x] MSIX package passes WACK validation
- [x] Store identity preserved (5999BirchCom.385071C3770F8)
- [x] Package submits to Partner Center successfully
- [x] App passes Store certification
- [x] App published and downloadable from Store

#### Performance
- [x] Startup time equal or better than UWP version
- [x] Memory usage acceptable (<500 MB typical)
- [x] No memory leaks during extended operation
- [x] Background polling executes reliably

### Quality Criteria

- [x] Code quality maintained (no major technical debt introduced)
- [x] Logging comprehensive (errors, warnings, info events captured)
- [x] Error handling robust (no unhandled exceptions)
- [x] Documentation updated (README, deployment guides)

### Process Criteria

- [x] All phases completed according to plan
- [x] Source control strategy followed (feature branch, atomic commits)
- [x] Code reviews completed for all major changes
- [x] Testing strategy executed (unit, integration, functional, kiosk)
- [x] Stakeholder approval received

### Migration Complete When

✅ **ALL** of the following are true:

1. **Technical**: Application builds, runs, and functions correctly on Windows App SDK
2. **Kiosk Mode**: Full kiosk functionality verified on test device
3. **Testing**: All automated and manual tests pass
4. **Store**: App published to Microsoft Store and downloadable
5. **Performance**: Meets or exceeds UWP baseline
6. **Quality**: No critical bugs or regressions
7. **Documentation**: All migration changes documented
8. **Approval**: Stakeholders approve release

---

**Migration Plan Complete**  
**Total Estimated Duration**: 3-4 weeks  
**Total Estimated Effort**: 85-127 hours  
**Risk Level**: MEDIUM (with mitigation strategies in place)

---

## Next Steps

1. **Review this plan** with development team and stakeholders
2. **Get approval** to proceed with migration
3. **Begin Phase 0** (Preparation & Prerequisites)
4. **Execute phases sequentially** following this plan
5. **Update plan** as needed based on discoveries during migration

**Questions or Concerns?** Address before beginning Phase 0.

**Ready to proceed?** Begin with Phase 0: Preparation & Prerequisites.

---

**Plan Status**: Phase 0 detailed, remaining phases in progress...
