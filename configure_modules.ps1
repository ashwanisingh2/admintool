$modules = @("SystemCleanup", "SoftwareManager", "PortableTools")
foreach ($mod in $modules) {
    $projPath = "src\Modules\SysAdminX.$mod\SysAdminX.$mod.csproj"
    
    # Update csproj
    $content = Get-Content $projPath -Raw
    $content = $content -replace '<OutputType>WinExe</OutputType>', '<OutputType>Library</OutputType>'
    Set-Content -Path $projPath -Value $content

    # Delete default files
    Remove-Item "src\Modules\SysAdminX.$mod\MainWindow.xaml" -Force -ErrorAction SilentlyContinue
    Remove-Item "src\Modules\SysAdminX.$mod\MainWindow.xaml.cs" -Force -ErrorAction SilentlyContinue
    Remove-Item "src\Modules\SysAdminX.$mod\App.xaml" -Force -ErrorAction SilentlyContinue
    Remove-Item "src\Modules\SysAdminX.$mod\App.xaml.cs" -Force -ErrorAction SilentlyContinue

    # Add to solution
    dotnet sln add $projPath

    # Add references
    dotnet add $projPath reference src\SysAdminX.Core\SysAdminX.Core.csproj
    dotnet add $projPath package CommunityToolkit.Mvvm --version 8.4.0
    dotnet add $projPath package WPF-UI --version 3.0.5

    # Add to shell and app
    dotnet add src\SysAdminX.Shell\SysAdminX.Shell.csproj reference $projPath
    dotnet add src\SysAdminX.App\SysAdminX.App.csproj reference $projPath

    # Create directories
    New-Item -ItemType Directory -Path "src\Modules\SysAdminX.$mod\Models" -Force | Out-Null
    New-Item -ItemType Directory -Path "src\Modules\SysAdminX.$mod\ViewModels" -Force | Out-Null
    New-Item -ItemType Directory -Path "src\Modules\SysAdminX.$mod\Views" -Force | Out-Null
}
