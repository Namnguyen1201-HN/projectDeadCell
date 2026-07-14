function Move-UnityFile {
    param(
        [string]$source,
        [string]$destFolder
    )
    if (Test-Path $source) {
        Move-Item -Path $source -Destination $destFolder -Force
        $meta = "$source.meta"
        if (Test-Path $meta) {
            Move-Item -Path $meta -Destination $destFolder -Force
        }
    } else {
        Write-Host "File not found: $source"
    }
}

$base = "d:\ProjectGame\projectDeadCell\Assets"
$scripts = "$base\Scripts"

# Create directories
New-Item -ItemType Directory -Force -Path "$scripts\Seasons\Autumn"
New-Item -ItemType Directory -Force -Path "$scripts\Seasons\Summer"
New-Item -ItemType Directory -Force -Path "$scripts\Enemies_Core"
New-Item -ItemType Directory -Force -Path "$base\Animations\Enemies\Minotaur"

# Move Core Enemies
Move-UnityFile "$scripts\Enemies\EnemyController.cs" "$scripts\Enemies_Core"
Move-UnityFile "$scripts\Enemies\BossController.cs" "$scripts\Enemies_Core"
Move-UnityFile "$scripts\Enemies\Projectile.cs" "$scripts\Enemies_Core"
Move-UnityFile "$scripts\Enemies\Boss\BossBase.cs" "$scripts\Enemies_Core"

# Move Summer (map2)
Move-UnityFile "$scripts\map2\Archer.cs" "$scripts\Seasons\Summer"
Move-UnityFile "$scripts\map2\ParallaxManager.cs" "$scripts\Seasons\Summer"

Get-ChildItem -Path "$scripts\map2" -Filter "*.anim" | ForEach-Object {
    Move-UnityFile $_.FullName "$base\Animations\Enemies\Minotaur"
}
Get-ChildItem -Path "$scripts\map2" -Filter "*.controller" | ForEach-Object {
    Move-UnityFile $_.FullName "$base\Animations\Enemies\Minotaur"
}

# Move Autumn specific files
Move-UnityFile "$scripts\Enemies\Boss\AutumnBoss.cs" "$scripts\Seasons\Autumn"
Move-UnityFile "$scripts\Enemies\Autumn\StealthGoblin.cs" "$scripts\Seasons\Autumn"
Move-UnityFile "$scripts\Enemies\Autumn\SwiftBat.cs" "$scripts\Seasons\Autumn"
Move-UnityFile "$scripts\Environment\AutumnFogEffect.cs" "$scripts\Seasons\Autumn"
Move-UnityFile "$scripts\Environment\AutumnLeafDrift.cs" "$scripts\Seasons\Autumn"
Move-UnityFile "$scripts\Environment\AutumnWindZone.cs" "$scripts\Seasons\Autumn"
Move-UnityFile "$scripts\Player\AutumnArcherVisualEnforcer.cs" "$scripts\Seasons\Autumn"
Move-UnityFile "$scripts\Player\AutumnRangerVisual.cs" "$scripts\Seasons\Autumn"

# Remove empty directories if empty
if (Test-Path "$scripts\map2") {
    if ((Get-ChildItem -Path "$scripts\map2").Count -eq 0) {
        Remove-Item -Path "$scripts\map2" -Force
        if (Test-Path "$scripts\map2.meta") { Remove-Item "$scripts\map2.meta" -Force }
    }
}

if (Test-Path "$scripts\Enemies\Autumn") {
    if ((Get-ChildItem -Path "$scripts\Enemies\Autumn").Count -eq 0) {
        Remove-Item -Path "$scripts\Enemies\Autumn" -Force
        if (Test-Path "$scripts\Enemies\Autumn.meta") { Remove-Item "$scripts\Enemies\Autumn.meta" -Force }
    }
}

Write-Host "Refactoring complete."
