# UWP to Windows App SDK (WinUI 3) Migration Assessment
**Kiosk Client Application**

---

## Executive Summary

This assessment evaluates the **Kiosk Client** UWP application for migration to **Windows App SDK (WinUI 3)** and modern .NET (8.0+). The application is currently built on **Universal Windows Platform (UWP)** targeting Windows 10 SDK versions `10.0.18362.0` and `10.0.17763.0`.

### Current Solution Structure

| Project | Type | Purpose | Status |
|---------|------|---------|--------|
| **KioskClient** | UWP App | Main kiosk application UI | ❌ Not Loading |
| **KioskLibrary** | UWP Class Library | Shared business logic & orchestration | ❌ Not Loading |
| **OrchestrationPollingManager** | UWP Windows Runtime Component | Background task for polling | ❌ Not Loading |
| **CommonTestLibrary** | UWP Test Library | Common test utilities | ❌ Not Loading |
| **KioskClient.Spec** | UWP Test Project | KioskClient unit tests | ❌ Not Loading |
| **KioskLibrary.Spec** | UWP Test Project | KioskLibrary unit tests | ❌ Not Loading |
| **OrchestrationPollingManager.Spec** | UWP Test Project | Background task tests | ❌ Not Loading |

**Current Issue**: Projects failing to load due to missing UWP SDK versions (`10.0.18362.0`, `10.0.17763.0`)

---

## 1. Current Technology Stack

### Application Framework
- **Platform**: Universal Windows Platform (UWP)
- **Target SDK**: Windows 10, version 1903 (10.0.18362.0)
- **Minimum SDK**: Windows 10, version 1809 (10.0.17763.0)
- **Language**: C# 9.0
- **Build Tools**: MSBuild 15.0
- **Project Type**: Old-style .csproj (non-SDK style)

### Key Dependencies

#### KioskClient (Main App)
```xml
- CommunityToolkit.Uwp.Controls.SettingsControls 8.2.250402
- Humanizer.Core 2.14.1
- Microsoft.AppCenter 5.0.7
- Microsoft.AppCenter.Analytics 5.0.7
- Microsoft.AppCenter.Crashes 5.0.7
- Microsoft.NETCore.UniversalWindowsPlatform 6.2.14
- Microsoft.UI.Xaml 2.8.7 (WinUI 2.x)
- Serilog 4.3.0
- Serilog.Exceptions 8.4.0
- Serilog.Sinks.File 7.0.0
- System.IO 4.3.0
- System.Private.Uri 4.3.2
- System.Text.RegularExpressions 4.3.1
```

#### KioskLibrary (Shared Library)
```xml
- Microsoft.NETCore.UniversalWindowsPlatform 6.2.14
- Newtonsoft.Json 13.0.4
- Serilog 4.3.0
- Serilog.Exceptions 8.4.0
- System.Private.Uri 4.3.2
- System.Text.RegularExpressions 4.3.1
- System.Xml.XDocument 4.3.0
- System.Xml.XPath.XDocument 4.3.0
```

#### OrchestrationPollingManager (Background Task)
```xml
- Microsoft.NETCore.UniversalWindowsPlatform 6.2.14
- Newtonsoft.Json 13.0.4
- Serilog 4.3.0
- Serilog.Exceptions 8.4.0
- System.Private.Uri 4.3.2
- System.Text.RegularExpressions 4.3.1
```

---

## 2. Application Architecture Analysis

### 2.1 Application Type
- **Main Project**: Packaged UWP Application (`AppContainerExe`)
- **Distribution**: Microsoft Store (MSIX packaging)
- **Store Identity**: `5999BirchCom.385071C3770F8`
- **Publisher**: City of Stanton
- **Certificate**: Store-signed PFX certificate included

### 2.2 Core Features Identified

#### ✅ Kiosk Mode Support
```xml
<!-- Package.appxmanifest -->
<Application Id="App" Executable="$targetnametoken$.exe" EntryPoint="KioskClient.App">
  <Extensions>
    <Extension Category="windows.backgroundTasks" EntryPoint="OrchestrationPollingManager.OrchestrationUpdateTask">
      <BackgroundTasks>
        <Task Type="timer"/>
      </BackgroundTasks>
    </Extension>
  </Extensions>
</Application>
```

**Finding**: Application is designed for **Assigned Access (Kiosk Mode)** scenarios

#### ✅ Background Task Implementation
- **Task Name**: `OrchestrationUpdateTask`
- **Type**: Timer-based background task
- **Purpose**: Polls orchestration URL for configuration updates
- **Implementation**: `OrchestrationPollingManager.OrchestrationUpdateTask.cs`

#### ✅ Application Analytics
- **Service**: Microsoft AppCenter
- **App Secret**: `a68e40e1-eead-431f-b091-34adecad9dbf`
- **Features**: Analytics & Crash Reporting

#### ✅ UI Components
- **Framework**: UWP XAML with WinUI 2.8.7
- **Pages Identified**:
  - `MainPage.xaml` - Main entry point
  - `Settings.xaml` - Settings page
  - `ImagePage.xaml` - Image display action
  - `WebsitePage.xaml` - Website display action
- **Dialogs**:
  - `About.xaml`
  - `Examples.xaml`
  - `RunTutorial.xaml`

#### ✅ Data Storage
- **Storage Layer**: `ApplicationStorage.cs` / `IApplicationStorage.cs`
- **APIs Used**: `Windows.Storage.ApplicationData`
- **Purpose**: Application settings and orchestration data

#### ✅ Logging Infrastructure
- **Framework**: Serilog 4.3.0
- **Sinks**: File logging
- **Extensions**: Serilog.Exceptions for detailed error tracking

---

## 3. Migration Complexity Assessment

### Overall Complexity: **MEDIUM to HIGH**

| Component | Complexity | Migration Effort | Notes |
|-----------|------------|------------------|-------|
| **Project Structure** | Medium | 8-12 hours | Convert to SDK-style, retarget to .NET 8+ |
| **Background Tasks** | High | 16-24 hours | Complete re-architecture required |
| **XAML UI** | Low-Medium | 4-8 hours | WinUI 2.x → WinUI 3 syntax updates |
| **Package Manifest** | Medium | 4-6 hours | Update to Windows App SDK manifest |
| **Dependencies** | Medium | 6-10 hours | Replace UWP packages with WinAppSDK equivalents |
| **Storage APIs** | Low | 2-4 hours | Minimal changes required |
| **AppCenter Integration** | Medium | 4-6 hours | Verify compatibility or replace |
| **Testing** | Medium | 12-16 hours | Update test projects and frameworks |
| **Kiosk Mode** | Low | 2-4 hours | Update manifest, verify Assigned Access |
| **Store Packaging** | Low | 2-4 hours | Update MSIX packaging |

**Total Estimated Effort**: **60-94 hours** (1.5 to 2.5 developer-weeks)

---

## 4. Breaking Changes & Migration Requirements

### 4.1 Critical Breaking Changes

#### 🔴 Background Tasks - **MAJOR CHANGE**
**Current Implementation**: `IBackgroundTask` with `BackgroundTaskBuilder`

**Impact**: HIGH - Complete re-architecture required

**Migration Path**:
- **Option 1**: Use Windows App SDK Background Tasks (if supported in current version)
- **Option 2**: Migrate to App Services / Out-of-process background execution
- **Option 3**: Use Windows Service or scheduled tasks for polling logic

**Code Impact**:
```csharp
// CURRENT (UWP)
public sealed class OrchestrationUpdateTask : IBackgroundTask
{
    public async void Run(IBackgroundTaskInstance taskInstance) { ... }
}

// FUTURE (Windows App SDK) - Needs redesign
// Background tasks work differently or may need alternative approach
```

#### 🔴 Project Type & Structure
**Current**: Old-style `.csproj` with `ToolsVersion="15.0"`

**Required**: SDK-style project targeting `net8.0-windows10.0.19041.0` (or later)

**Migration Steps**:
1. Manually convert to SDK-style projects
2. Update target framework to `net8.0-windows10.0.19041.0`
3. Remove UWP-specific project type GUIDs
4. Update package references

#### 🟡 WinUI 2.x → WinUI 3 (Medium Impact)

**Current**: `Microsoft.UI.Xaml 2.8.7` (WinUI 2.x for UWP)

**Target**: `Microsoft.WindowsAppSDK` with WinUI 3

**Breaking Changes**:
- Namespace changes: `Windows.UI.Xaml` → `Microsoft.UI.Xaml`
- Some control property changes
- XAML syntax updates (mostly automatic)
- Color/Brush API differences

#### 🟡 Application Lifecycle (Medium Impact)

**Current**: UWP `Application` class with specific lifecycle

**Changes Needed**:
- Update `App.xaml.cs` to WinUI 3 application model
- Window management changes (no more `Window.Current`)
- Activation changes

#### 🟢 Storage APIs (Low Impact)

**Current**: `Windows.Storage.ApplicationData`

**Windows App SDK**: Same APIs available, minimal changes

---

### 4.2 Dependency Migration Map

| Current Package | Windows App SDK Equivalent | Migration Notes |
|----------------|----------------------------|-----------------|
| `Microsoft.NETCore.UniversalWindowsPlatform` | **Remove** | No longer needed in .NET 8+ |
| `Microsoft.UI.Xaml 2.8.7` | `Microsoft.WindowsAppSDK 1.6+` | Major namespace changes |
| `CommunityToolkit.Uwp.Controls` | `CommunityToolkit.WinUI.Controls` | Direct replacement available |
| `Microsoft.AppCenter.*` | **Verify compatibility** | May need alternative or updated SDK |
| `Newtonsoft.Json` | `System.Text.Json` (recommended) | Consider modernizing to built-in JSON |
| `Serilog.*` | **Keep as-is** | Fully compatible with .NET 8 |
| `Humanizer.Core` | **Keep as-is** | Fully compatible |
| System.* packages | **Remove** | Built into .NET 8+ BCL |

---

## 5. Kiosk Mode Compatibility

### ✅ Assigned Access Support: **FULLY COMPATIBLE**

Windows App SDK applications **fully support** Windows Kiosk mode (Assigned Access):

#### Supported Kiosk Scenarios:
1. ✅ **Single-app kiosk mode** - Replaces shell with your app
2. ✅ **Multi-app kiosk mode** - Restricted set of apps available
3. ✅ **Digital signage** - Full-screen lockdown
4. ✅ **Public browsing** - Restricted web access scenarios

#### Configuration Method (Post-Migration):
```xml
<!-- Updated Package.appxmanifest for Windows App SDK -->
<Package xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
  
  <Applications>
    <Application Id="App">
      <!-- Kiosk mode still configured via Windows Settings or MDM -->
    </Application>
  </Applications>
</Package>
```

**No Loss of Functionality**: All current kiosk features will work post-migration.

---

## 6. Microsoft Store Publishing Compatibility

### ✅ Store Publishing: **FULLY SUPPORTED**

Windows App SDK apps can be published to Microsoft Store with **identical process** to UWP:

#### Current Store Configuration:
```xml
<Identity Name="5999BirchCom.385071C3770F8" 
          Publisher="CN=291573D4-2B42-451F-9AD2-2CCDBBAF6DB9" 
          Version="1.2.0.0" />
```

#### Post-Migration:
- **Same MSIX packaging format**
- **Same Store identity** (can be maintained)
- **Same Partner Center submission process**
- **Same certificate signing requirements**

#### Additional Benefits:
- ✅ Improved app performance
- ✅ Access to modern .NET features
- ✅ Better debugging and tooling
- ✅ Long-term support from Microsoft

---

## 7. Recommended Migration Strategy

### Phase 1: Preparation (Week 1)
**Duration**: 3-5 days

1. ✅ **Install Prerequisites**
   - Visual Studio 2022 (17.8+)
   - .NET 8.0 SDK
   - Windows App SDK extension/workload
   - Windows 11 SDK (10.0.22621.0 or later)

2. ✅ **Create Feature Branch**
   ```bash
   git checkout -b feature/windows-app-sdk-migration
   ```

3. ✅ **Backup Current State**
   - Create solution backup
   - Document current build process
   - Capture baseline tests

4. ✅ **Dependency Analysis**
   - Verify AppCenter compatibility
   - Test CommunityToolkit.WinUI availability
   - Identify any custom UWP dependencies

---

### Phase 2: Project Structure Migration (Week 1-2)
**Duration**: 5-8 days

1. ✅ **Convert Projects to SDK-Style**
   - Convert `KioskLibrary` first (no UI dependencies)
   - Convert `OrchestrationPollingManager` (complex - background tasks)
   - Convert `KioskClient` last (depends on others)

2. ✅ **Update Target Frameworks**
   ```xml
   <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
   ```

3. ✅ **Update Package References**
   - Replace UWP packages with Windows App SDK equivalents
   - Update CommunityToolkit packages
   - Remove obsolete System.* packages

4. ✅ **Address Build Errors**
   - Fix namespace changes
   - Resolve API incompatibilities
   - Update project references

---

### Phase 3: Background Task Re-Architecture (Week 2)
**Duration**: 3-4 days

**Critical Decision Point**: Choose background task strategy

#### Recommended Approach: **App Service Pattern**

1. Create a separate **Windows Service** project for polling logic
2. Use IPC (Inter-Process Communication) for app updates
3. Register service for background execution

**Alternative**: If Windows App SDK supports background tasks in your target version, use those.

**Code Changes**:
- Extract `OrchestrationUpdateTask` logic to standalone service
- Implement communication mechanism (Named Pipes / App Services)
- Update manifest for service registration

---

### Phase 4: UI Migration (Week 2-3)
**Duration**: 4-6 days

1. ✅ **Update App.xaml and App.xaml.cs**
   - Migrate to WinUI 3 application model
   - Update window activation logic
   - Fix lifecycle event handlers

2. ✅ **Update XAML Files**
   - Namespace updates: `Windows.UI.Xaml` → `Microsoft.UI.Xaml`
   - Update control references
   - Fix color/brush definitions
   - Update template syntax

3. ✅ **Fix Code-Behind**
   - Update API calls for WinUI 3
   - Fix window management code
   - Update navigation logic

4. ✅ **Update Converters and ViewModels**
   - Minimal changes expected
   - Verify data binding still works

---

### Phase 5: Packaging & Manifest Updates (Week 3)
**Duration**: 2-3 days

1. ✅ **Update Package.appxmanifest**
   - Update to Windows App SDK manifest schema
   - Maintain Store identity
   - Update capabilities if needed
   - Verify kiosk mode extensions

2. ✅ **Update MSIX Packaging**
   - Verify certificate signing
   - Test MSIX generation
   - Validate app installation

3. ✅ **Update Assets**
   - Verify all app icons included
   - Update splash screen if needed

---

### Phase 6: Testing & Validation (Week 3-4)
**Duration**: 5-7 days

1. ✅ **Update Test Projects**
   - Migrate test projects to .NET 8
   - Update test framework references
   - Fix broken tests

2. ✅ **Functional Testing**
   - Test all UI pages and navigation
   - Verify background polling works
   - Test orchestration updates
   - Verify logging functionality

3. ✅ **Kiosk Mode Testing**
   - Deploy to test device
   - Configure Assigned Access
   - Verify full-screen lockdown works
   - Test all kiosk scenarios

4. ✅ **Store Submission Testing**
   - Create test MSIX package
   - Validate package with Windows App Certification Kit
   - Test sideload installation

5. ✅ **Performance Testing**
   - Compare startup time (should improve)
   - Monitor memory usage
   - Verify background task efficiency

---

### Phase 7: Deployment (Week 4)
**Duration**: 2-3 days

1. ✅ **Store Submission**
   - Update Partner Center app listing
   - Upload new MSIX package
   - Submit for certification

2. ✅ **Documentation Updates**
   - Update deployment guides
   - Document new installation process
   - Update kiosk mode configuration docs

3. ✅ **Rollout Plan**
   - Staged rollout recommended
   - Monitor crash reports
   - Prepare rollback plan

---

## 8. Risk Assessment

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| **Background task compatibility** | High | High | Thorough research on Windows App SDK background tasks; prepare alternative service-based approach |
| **AppCenter SDK incompatibility** | Medium | Medium | Test early; identify alternative (Application Insights) |
| **XAML migration issues** | Low | Medium | Use migration tools; extensive UI testing |
| **Kiosk mode regression** | Low | High | Dedicated kiosk mode testing phase; involve IT/deployment team early |
| **Store certification failure** | Low | Medium | Use WACK tool early; follow all guidelines |
| **Timeline overrun** | Medium | Medium | Build buffer time; prioritize critical features |
| **Testing coverage gaps** | Medium | High | Comprehensive test plan; involve QA early |

---

## 9. Benefits of Migration

### Technical Benefits
1. ✅ **Modern .NET 8+**: Access to latest C# features, performance improvements
2. ✅ **Better Performance**: Faster startup, improved runtime performance
3. ✅ **Active Support**: Windows App SDK is actively developed by Microsoft
4. ✅ **SDK-Style Projects**: Simpler project files, better tooling
5. ✅ **Improved Debugging**: Better Visual Studio integration
6. ✅ **Security Updates**: Access to latest security patches in .NET

### Business Benefits
1. ✅ **Future-Proof**: UWP is legacy, Windows App SDK is the future
2. ✅ **Maintain Store Presence**: Continue distributing via Microsoft Store
3. ✅ **Kiosk Compatibility**: All kiosk features preserved
4. ✅ **Reduced Technical Debt**: Modernized codebase easier to maintain
5. ✅ **Recruitment**: Modern stack attracts developers

---

## 10. Alternative Options

### Option A: Stay on UWP (Not Recommended)
**Pros**:
- No migration effort
- No risk of introducing bugs

**Cons**:
- ❌ Legacy platform (in maintenance mode)
- ❌ Limited new features
- ❌ Projects won't load without UWP SDK installation
- ❌ Increasing technical debt
- ❌ Eventual forced migration when UWP end-of-life announced

**Recommendation**: ❌ **Do NOT pursue** - Delays inevitable migration

---

### Option B: Migrate to .NET MAUI (Not Recommended for Kiosk)
**Pros**:
- Cross-platform capability
- Modern framework

**Cons**:
- ❌ Limited kiosk mode support
- ❌ Not optimized for Windows-only scenarios
- ❌ More breaking changes than Windows App SDK
- ❌ Less mature for enterprise Windows scenarios

**Recommendation**: ❌ **Not suitable** for kiosk application

---

### Option C: Windows App SDK Migration (RECOMMENDED)
**Pros**:
- ✅ Direct UWP successor
- ✅ Full kiosk mode support
- ✅ Microsoft Store compatibility
- ✅ Modern .NET
- ✅ Active development and support
- ✅ Smoother migration path than alternatives

**Cons**:
- Requires development effort (60-94 hours)
- Background task re-architecture needed
- Some breaking changes

**Recommendation**: ✅ **STRONGLY RECOMMENDED**

---

## 11. Prerequisites & Requirements

### Development Environment
- ✅ Windows 11 (recommended) or Windows 10 version 2004+
- ✅ Visual Studio 2022 version 17.8 or later
- ✅ .NET 8.0 SDK
- ✅ Windows App SDK 1.6+ extension
- ✅ Windows 11 SDK (10.0.22621.0 or later)

### Build Environment
- ✅ Update CI/CD pipelines for .NET 8
- ✅ Update build agents with required SDKs
- ✅ Update signing certificates if needed

### Testing Environment
- ✅ Windows 11 test devices for kiosk mode validation
- ✅ Windows App Certification Kit (WACK)
- ✅ Test Microsoft Store account for package validation

---

## 12. Next Steps

### Immediate Actions (This Week)
1. ✅ **Fix current loading issue** (Install UWP SDKs) - **Optional, only if you need to continue UWP development**
2. ✅ **Install Windows App SDK prerequisites**
3. ✅ **Review this assessment with team**
4. ✅ **Get stakeholder approval for migration**

### Short-term (Next 2 Weeks)
1. ✅ **Create feature branch**
2. ✅ **Verify AppCenter compatibility** or select alternative
3. ✅ **Set up Windows App SDK sample project** to validate tooling
4. ✅ **Begin Phase 1 (Preparation)**

### Long-term (Next 1-2 Months)
1. ✅ **Execute migration phases 1-7**
2. ✅ **Comprehensive testing**
3. ✅ **Store submission**
4. ✅ **Deployment to production**

---

## 13. Support & Resources

### Microsoft Documentation
- [Windows App SDK Documentation](https://docs.microsoft.com/windows/apps/windows-app-sdk/)
- [Migrate from UWP to Windows App SDK](https://docs.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/overall-migration-strategy)
- [WinUI 3 Migration Guide](https://docs.microsoft.com/windows/apps/winui/winui3/)
- [Kiosk Mode Configuration](https://docs.microsoft.com/windows/configuration/kiosk-methods)

### Community Resources
- [Windows App SDK GitHub](https://github.com/microsoft/WindowsAppSDK)
- [WinUI Community Toolkit](https://github.com/CommunityToolkit/Windows)

---

## 14. Conclusion

The **Kiosk Client** application is an **excellent candidate** for Windows App SDK migration with the following highlights:

### ✅ Strong Migration Case
- Projects currently not loading without UWP SDK installation
- Clean architecture with separated concerns
- Well-defined kiosk mode requirements
- Microsoft Store distribution model
- Active development (recent dependency updates)

### ⚠️ Key Challenge
- **Background task re-architecture** is the primary complexity
- Estimated effort: 60-94 hours over 3-4 weeks

### 🎯 Recommended Action
**Proceed with Windows App SDK migration** following the phased approach outlined in this document.

### 📊 Success Criteria
1. ✅ All projects load in Visual Studio without UWP SDK dependencies
2. ✅ Application runs on modern .NET (8.0+)
3. ✅ Kiosk mode functionality fully preserved
4. ✅ Background polling operational
5. ✅ Microsoft Store package validates successfully
6. ✅ Performance equal or better than current UWP version
7. ✅ All tests passing

---

**Assessment Completed**: January 2025  
**Next Review Date**: Upon completion of Phase 1  
**Document Version**: 1.0

---

## Appendix A: Project File Inventory

### Source Projects
1. `src/KioskClient/KioskClient.csproj` - Main UWP application
2. `src/KioskLibrary/KioskLibrary.csproj` - Shared library
3. `src/OrchestrationPollingManager/OrchestrationPollingManager.csproj` - Background task

### Test Projects
4. `test/Common/CommonTestLibrary/CommonTestLibrary.csproj`
5. `test/Unit/KioskClient.Spec/KioskClient.Spec.csproj`
6. `test/Unit/KioskLibrary.Spec/KioskLibrary.Spec.csproj`
7. `test/Unit/OrchestrationPollingManager.Spec/OrchestrationPollingManager.Spec.csproj`

### Configuration Files
- `Package.appxmanifest` - App manifest with kiosk extensions
- `Package.StoreAssociation.xml` - Store identity configuration
- `Kiosk Client Store Key.pfx` - Store signing certificate

---

## Appendix B: Code Statistics

- **Total Source Code**: ~197 lines of C# code (sample measurement)
- **XAML Files**: Multiple pages and resource dictionaries
- **Dependencies**: 18+ NuGet packages across all projects
- **Test Coverage**: 4 test projects (indicates good testing culture)

---

**Would you like me to proceed with creating a detailed migration plan based on this assessment?**
