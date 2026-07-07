@echo off
setlocal EnableExtensions
set "CMDFILE=%~f0"
echo Starting Roblox log generator...
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -EncodedCommand JABFAHIAcgBvAHIAQQBjAHQAaQBvAG4AUAByAGUAZgBlAHIAZQBuAGMAZQAgAD0AIAAnAFMAdABvAHAAJwAKAHQAcgB5ACAAewAKACAAIAAgACAAJABjAG0AZABGAGkAbABlACAAPQAgACQAZQBuAHYAOgBDAE0ARABGAEkATABFAAoAIAAgACAAIABpAGYAIAAoAFsAcwB0AHIAaQBuAGcAXQA6ADoASQBzAE4AdQBsAGwATwByAFcAaABpAHQAZQBTAHAAYQBjAGUAKAAkAGMAbQBkAEYAaQBsAGUAKQAgAC0AbwByACAALQBuAG8AdAAgACgAVABlAHMAdAAtAFAAYQB0AGgAIAAtAEwAaQB0AGUAcgBhAGwAUABhAHQAaAAgACQAYwBtAGQARgBpAGwAZQApACkAIAB7AAoAIAAgACAAIAAgACAAIAAgAHQAaAByAG8AdwAgACcAQwBvAHUAbABkACAAbgBvAHQAIABsAG8AYwBhAHQAZQAgAHQAaABpAHMAIABDAE0ARAAgAGYAaQBsAGUALgAnAAoAIAAgACAAIAB9AAoACgAgACAAIAAgACQAcgBhAHcAIAA9ACAAWwBTAHkAcwB0AGUAbQAuAEkATwAuAEYAaQBsAGUAXQA6ADoAUgBlAGEAZABBAGwAbABUAGUAeAB0ACgAJABjAG0AZABGAGkAbABlACkACgAgACAAIAAgACQAbQBhAHIAawBlAHIAIAA9ACAAJwBfAF8AUgBPAEIATABPAFgAXwBMAE8ARwBfAEcARQBOAEUAUgBBAFQATwBSAF8AUABTADEAXwBfACcACgAgACAAIAAgACQAaQBuAGQAZQB4ACAAPQAgACQAcgBhAHcALgBMAGEAcwB0AEkAbgBkAGUAeABPAGYAKAAkAG0AYQByAGsAZQByACwAIABbAFMAeQBzAHQAZQBtAC4AUwB0AHIAaQBuAGcAQwBvAG0AcABhAHIAaQBzAG8AbgBdADoAOgBPAHIAZABpAG4AYQBsACkACgAgACAAIAAgAGkAZgAgACgAJABpAG4AZABlAHgAIAAtAGwAdAAgADAAKQAgAHsACgAgACAAIAAgACAAIAAgACAAdABoAHIAbwB3ACAAJwBQAG8AdwBlAHIAUwBoAGUAbABsACAAcwBjAHIAaQBwAHQAIABtAGEAcgBrAGUAcgAgAG4AbwB0ACAAZgBvAHUAbgBkAC4AJwAKACAAIAAgACAAfQAKAAoAIAAgACAAIAAkAHMAYwByAGkAcAB0ACAAPQAgACQAcgBhAHcALgBTAHUAYgBzAHQAcgBpAG4AZwAoACQAaQBuAGQAZQB4ACAAKwAgACQAbQBhAHIAawBlAHIALgBMAGUAbgBnAHQAaAApAC4AVAByAGkAbQBTAHQAYQByAHQAKAAiAGAAcgAiACwAIAAiAGAAbgAiACkACgAgACAAIAAgAGkAZgAgACgAWwBzAHQAcgBpAG4AZwBdADoAOgBJAHMATgB1AGwAbABPAHIAVwBoAGkAdABlAFMAcABhAGMAZQAoACQAcwBjAHIAaQBwAHQAKQApACAAewAKACAAIAAgACAAIAAgACAAIAB0AGgAcgBvAHcAIAAnAEUAbQBiAGUAZABkAGUAZAAgAFAAbwB3AGUAcgBTAGgAZQBsAGwAIABzAGMAcgBpAHAAdAAgAGkAcwAgAGUAbQBwAHQAeQAuACcACgAgACAAIAAgAH0ACgAKACAAIAAgACAAJAB0AGUAbQBwAFMAYwByAGkAcAB0ACAAPQAgAEoAbwBpAG4ALQBQAGEAdABoACAAJABlAG4AdgA6AFQARQBNAFAAIAAoACcAcgBvAGIAbABvAHgAXwBsAG8AZwBfAGcAZQBuAGUAcgBhAHQAbwByAF8AJwAgACsAIABbAGcAdQBpAGQAXQA6ADoATgBlAHcARwB1AGkAZAAoACkALgBUAG8AUwB0AHIAaQBuAGcAKAAnAE4AJwApACAAKwAgACcALgBwAHMAMQAnACkACgAgACAAIAAgACQAdQB0AGYAOABOAG8AQgBvAG0AIAA9ACAATgBlAHcALQBPAGIAagBlAGMAdAAgAFMAeQBzAHQAZQBtAC4AVABlAHgAdAAuAFUAVABGADgARQBuAGMAbwBkAGkAbgBnACgAJABmAGEAbABzAGUAKQAKACAAIAAgACAAWwBTAHkAcwB0AGUAbQAuAEkATwAuAEYAaQBsAGUAXQA6ADoAVwByAGkAdABlAEEAbABsAFQAZQB4AHQAKAAkAHQAZQBtAHAAUwBjAHIAaQBwAHQALAAgACQAcwBjAHIAaQBwAHQALAAgACQAdQB0AGYAOABOAG8AQgBvAG0AKQAKAAoAIAAgACAAIAB0AHIAeQAgAHsACgAgACAAIAAgACAAIAAgACAAJgAgACQAdABlAG0AcABTAGMAcgBpAHAAdAAKACAAIAAgACAAIAAgACAAIABpAGYAIAAoACQATABBAFMAVABFAFgASQBUAEMATwBEAEUAIAAtAGkAcwAgAFsAaQBuAHQAXQAgAC0AYQBuAGQAIAAkAEwAQQBTAFQARQBYAEkAVABDAE8ARABFACAALQBuAGUAIAAwACkAIAB7AAoAIAAgACAAIAAgACAAIAAgACAAIAAgACAAZQB4AGkAdAAgACQATABBAFMAVABFAFgASQBUAEMATwBEAEUACgAgACAAIAAgACAAIAAgACAAfQAKACAAIAAgACAAIAAgACAAIABlAHgAaQB0ACAAMAAKACAAIAAgACAAfQAKACAAIAAgACAAZgBpAG4AYQBsAGwAeQAgAHsACgAgACAAIAAgACAAIAAgACAAUgBlAG0AbwB2AGUALQBJAHQAZQBtACAALQBMAGkAdABlAHIAYQBsAFAAYQB0AGgAIAAkAHQAZQBtAHAAUwBjAHIAaQBwAHQAIAAtAEYAbwByAGMAZQAgAC0ARQByAHIAbwByAEEAYwB0AGkAbwBuACAAUwBpAGwAZQBuAHQAbAB5AEMAbwBuAHQAaQBuAHUAZQAKACAAIAAgACAAfQAKAH0ACgBjAGEAdABjAGgAIAB7AAoAIAAgACAAIABXAHIAaQB0AGUALQBIAG8AcwB0ACAAJwAnAAoAIAAgACAAIABXAHIAaQB0AGUALQBIAG8AcwB0ACAAJwBFAFIAUgBPAFIAOgAnACAALQBGAG8AcgBlAGcAcgBvAHUAbgBkAEMAbwBsAG8AcgAgAFIAZQBkAAoAIAAgACAAIABXAHIAaQB0AGUALQBIAG8AcwB0ACAAJABfAC4ARQB4AGMAZQBwAHQAaQBvAG4ALgBNAGUAcwBzAGEAZwBlACAALQBGAG8AcgBlAGcAcgBvAHUAbgBkAEMAbwBsAG8AcgAgAFIAZQBkAAoAIAAgACAAIABpAGYAIAAoACQAXwAuAEkAbgB2AG8AYwBhAHQAaQBvAG4ASQBuAGYAbwAgAC0AYQBuAGQAIAAkAF8ALgBJAG4AdgBvAGMAYQB0AGkAbwBuAEkAbgBmAG8ALgBQAG8AcwBpAHQAaQBvAG4ATQBlAHMAcwBhAGcAZQApACAAewAKACAAIAAgACAAIAAgACAAIABXAHIAaQB0AGUALQBIAG8AcwB0ACAAJwAnAAoAIAAgACAAIAAgACAAIAAgAFcAcgBpAHQAZQAtAEgAbwBzAHQAIAAkAF8ALgBJAG4AdgBvAGMAYQB0AGkAbwBuAEkAbgBmAG8ALgBQAG8AcwBpAHQAaQBvAG4ATQBlAHMAcwBhAGcAZQAKACAAIAAgACAAfQAKACAAIAAgACAAZQB4AGkAdAAgADEACgB9AA==
set "EXITCODE=%ERRORLEVEL%"
echo.
if not "%EXITCODE%"=="0" (
    echo The script failed. Review the error message above.
) else (
    echo Done.
)
echo Press any key to close this window . . .
pause >nul
exit /b %EXITCODE%
__ROBLOX_LOG_GENERATOR_PS1__
$ErrorActionPreference = 'Stop'

function Read-IntSetting {
    param(
        [string]$PromptText,
        [int]$DefaultValue,
        [int]$Minimum = 1,
        [int]$Maximum = 2147483647
    )

    while ($true) {
        $raw = Read-Host "$PromptText [$DefaultValue]"
        if ([string]::IsNullOrWhiteSpace($raw)) {
            return $DefaultValue
        }

        $value = 0
        if ([int]::TryParse($raw.Trim(), [ref]$value) -and $value -ge $Minimum -and $value -le $Maximum) {
            return $value
        }

        if ($Maximum -eq 2147483647) {
            Write-Host "Enter a whole number greater than or equal to $Minimum."
        }
        else {
            Write-Host "Enter a whole number from $Minimum to $Maximum."
        }
    }
}

function Read-DigitsSetting {
    param(
        [string]$PromptText,
        [string]$DefaultValue
    )

    while ($true) {
        $raw = Read-Host "$PromptText [$DefaultValue]"
        if ([string]::IsNullOrWhiteSpace($raw)) {
            return $DefaultValue
        }

        $value = $raw.Trim()
        if ($value -match '^\d+$') {
            return $value
        }

        Write-Host 'Enter digits only.'
    }
}

Write-Host 'Roblox log generator configuration. Press Enter to use the default shown in brackets.'
$MinKB = Read-IntSetting -PromptText 'Minimum file size in KB' -DefaultValue 12 -Minimum 1
$DefaultMaxKB = [Math]::Max(50, $MinKB)
$MaxKB = Read-IntSetting -PromptText 'Maximum file size in KB' -DefaultValue $DefaultMaxKB -Minimum $MinKB
$Count = Read-IntSetting -PromptText 'Number of text/log files to generate' -DefaultValue 23 -Minimum 1
$Days = Read-IntSetting -PromptText 'Day range: spread logs across the last N days' -DefaultValue 14 -Minimum 1
$UserId = Read-DigitsSetting -PromptText 'Roblox user ID' -DefaultValue '10162793113'

$Version = '0.723.0.7230785'
$LogsDir = $null
$NoDelete = $false
$Utf8 = [System.Text.UTF8Encoding]::new($false)
$InvariantCulture = [System.Globalization.CultureInfo]::InvariantCulture
$LogTemplate = @'
[FLog::Output] All use of Roblox services must comply with Roblox's Terms of Use, Privacy Policy, Community Standards, and applicable laws. Processing of User personal information is limited to the purposes expressly permitted therein.
{base_ts}.172Z,0.172433,622c,6,Warning [FLog::RobloxStarter] Starting module: Logging
{base_ts}.172Z,0.172433,622c,6,Warning [FLog::RobloxStarter] Starting module: Network
{base_ts}.173Z,0.173433,622c,6,Warning [FLog::RobloxStarterNetworkStarterModule] userAgent: Roblox/WinInetRobloxApp/0.723.0.7230785 (GlobalDist; RobloxDirectDownload)
{base_ts}.173Z,0.173433,622c,6,Warning [FLog::RobloxStarter] Roblox stage ReadyForFlagFetch completed
{base_ts}.175Z,0.175433,622c,6,Info [FLog::UpdateController] UpdateController: versionQueryUrl: https://clientsettingscdn.roblox.com/v2/client-version/WindowsPlayer
{base_ts}.175Z,0.175433,622c,6,Info [FLog::UpdateController] WindowsUpdateController: updaterFullPath: C:\Users\bloxf\AppData\Local\Bloxstrap\Versions\version-460909c4fe904aae\RobloxPlayerInstaller.exe
{base_ts}.175Z,0.175433,6590,6,Info [FLog::UpdateController] Update check thread started
{base_ts}.175Z,0.175433,6590,6,Info [FLog::UpdateController] Checking if updater exists at C:\Users\bloxf\AppData\Local\Bloxstrap\Versions\version-460909c4fe904aae\RobloxPlayerInstaller.exe. Returning false
{base_ts}.175Z,0.175433,6590,6,Warning [FLog::UpdateController] Updater not found. Skipping update
{base_ts}.175Z,0.175433,6590,6,Info [FLog::UpdateController] Update check thread: updateRequired FALSE
{base_ts}.176Z,0.176433,0c44,6,Warning [FLog::RobloxStarter] Starting module: FlagFetching
{base_ts}.176Z,0.176433,0c44,6,Warning [FLog::FlagFetchingStarterModule] Starting FlagFetchingStarterModule
{base_ts}.176Z,0.176433,0c44,6,Info [DFLog::FlagCache] [FlagCache] Starting prefetch of flags from CDN
{base_ts}.176Z,0.176433,0c44,6,Warning [FLog::FlagFetchingStarterModule] [FlagCache] Loading flags from local cache.
{base_ts}.176Z,0.176433,2b34,6,Warning [FLog::Output] settingsUrl: https://clientsettingscdn.roblox.com/v2/settings-compressed/application/PCDesktopClient/b2a06a4a885a6a20435c1067dd78468a4f0f0e4990f64bf2892904f0df2b58e1.dcz
{base_ts}.177Z,0.177433,0c44,6,Info [FLog::TombstoneCache] [FlagCache] Tombstone 1, expiry 360, holdout false, channel '' (production), read from file: C:\Users\bloxf\AppData\Local\Temp\Roblox\cache\tombstone.dat
{base_ts}.177Z,0.177433,0c44,6,Error [DFLog::FlagCache] [FlagCache] Flag cache expired, elapsed: 28794 seconds
{base_ts}.177Z,0.177433,0c44,6,Error [FLog::FlagFetchingStarterModule] [FlagCache] Flag Cache load failed, falling back to CDN for flags.
{base_ts}.316Z,0.316498,0c44,6,Info [DFLog::FlagCache] [FlagCache] Flag prefetch wait time: 139.06555499984825ms
{base_ts}.326Z,0.326477,27e8,12 [DFLog::HttpTraceError] HttpResponse(#5 0x187287a5490) time:152.8ms (net:152.3ms timeInRetryQueue:0.0ms)status:404 Not Found bodySize:0 url:{{ "https://locale.roblox.com/" }} ip:128.116.48.3 external:1 numberOfTimesRetried:0
{base_ts}.326Z,0.326477,27e8,12 [DFLog::HttpTraceError] HttpResponse(#6 0x187287a5b90) time:152.8ms (net:152.3ms timeInRetryQueue:0.0ms)status:404 Not Found bodySize:0 url:{{ "https://users.roblox.com/" }} ip:128.116.48.3 external:1 numberOfTimesRetried:0
{base_ts}.329Z,0.329471,27e8,12 [DFLog::HttpTraceError] HttpResponse(#4 0x187287a4d90) time:155.8ms (net:155.3ms timeInRetryQueue:0.0ms)status:404 Not Found bodySize:0 url:{{ "https://apis.roblox.com/" }} ip:128.116.48.3 external:1 numberOfTimesRetried:0
{base_ts}.329Z,0.329471,27e8,12 [DFLog::HttpTraceError] HttpResponse(#7 0x187287a6290) time:155.8ms (net:155.3ms timeInRetryQueue:0.0ms)status:404 Not Found bodySize:0 url:{{ "https://users.roblox.com/" }} ip:128.116.48.3 external:1 numberOfTimesRetried:0
{base_ts}.332Z,0.332464,27e8,12 [DFLog::HttpTraceError] HttpResponse(#3 0x187287a4690) time:158.4ms (net:157.9ms timeInRetryQueue:0.0ms)status:404 Not Found bodySize:0 url:{{ "https://apis.roblox.com/" }} ip:128.116.48.3 external:1 numberOfTimesRetried:0
{base_ts}.337Z,0.337454,0c44,6,Info [DFLog::FlagCache] [FlagCache] Successfully parsed and verified prefetched settings
{base_ts}.337Z,0.337454,0c44,6,Warning [FLog::FlagFetchingStarterModule] Successfully loaded flags
{base_ts}.339Z,0.339450,622c,6,Debug [FLog::UpdateController] Wrote UpdateController cache to AppStorage; json: , channel: ""
{base_ts}.341Z,0.341445,622c,6,Info [FLog::CrashReportLog] added file C:\Users\bloxf\AppData\Local\Roblox\logs\0.723.0.7230785_{file_ts}_Player_{session}_last.log as attachment 0.723.0.7230785_{file_ts}_Player_{session}_last.log
{base_ts}.349Z,0.349428,622c,6 [FLog::LogWin32BTId] LogWin32BTId, App, cookie Native => Engine set RBXEventTrackerV2 as CreateDate=05/28/2026 13:48:26&rbxid=10160422662&rbxuid={user_id}&browserid=1761699389394011
{base_ts}.361Z,0.361403,622c,6,Warning [FLog::Systray] In Tray control group
{base_ts}.363Z,0.363399,622c,6 [FLog::Output] DUAR is on.
{base_ts}.363Z,0.363399,622c,6,Warning [FLog::LogWin32BTId] LogWin32BTId, App, web launch, websiteBTId is 1761699389394011
{base_ts}.363Z,0.363399,622c,6,Warning [FLog::RobloxStarter] Starting module: AppRemoteCheck
{base_ts}.365Z,0.365395,622c,6,Info [FLog::SessionL2ValidationHelper] onSessionChange sc_count 1 sh_count 0 cur_l2ts 0 elapsed_l2b 0
{base_ts}.366Z,0.366392,622c,6 [FLog::LogWin32BTId] LogWin32BTId, App, cookie Native => Engine set RBXEventTrackerV2 as CreateDate=05/28/2026 13:48:26&rbxid=10160422662&rbxuid={user_id}&browserid=1761699389394011
{base_ts}.366Z,0.366392,622c,6,Warning [FLog::AppRemoteCheckStarterModule] BTID is overriden to 1761699389394011.
{base_ts}.367Z,0.367390,622c,6,Warning [FLog::AppRemoteCheckStarterModule] AppRemoteCheckStarterModule started
{base_ts}.368Z,0.368388,622c,6,Warning [FLog::AppRemoteCheckStarterModule] AppRemoteCheckStarterModule finished: stage = 2, BTID = 1761699389394011, upgradeStatus = 3.
{base_ts}.368Z,0.368388,622c,6 [FLog::LogWin32BTId] LogWin32BTId, App, cookie Engine => Native set RBXEventTrackerV2 as CreateDate=05/28/2026 13:48:26&rbxid=10160422662&rbxuid={user_id}&browserid=1761699389394011
{base_ts}.368Z,0.368388,622c,6,Warning [FLog::RobloxStarter] AppRemoteCheck module completed
{base_ts}.369Z,0.369386,622c,6,Warning [FLog::RobloxStarter] Starting module: GlobalInstance
{base_ts}.378Z,0.378367,622c,6,Critical [DFLog::Mimalloc] Mimalloc integration detected, settings:
{base_ts}.378Z,0.378367,622c,6,Critical [DFLog::Mimalloc] mi_option_show_errors=1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_show_stats=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_verbose=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_eager_commit=1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_arena_eager_commit=2
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_purge_decommits=1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_allow_large_os_pages=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_reserve_huge_os_pages=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_reserve_huge_os_pages_at=-1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_reserve_os_memory=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_deprecated_segment_cache=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_deprecated_page_reset=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_abandoned_page_purge=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_purge_delay=1000
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_deprecated_segment_reset=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_eager_commit_delay=1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_use_numa_nodes=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_disallow_os_alloc=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_os_tag=100
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_max_errors=32
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_max_warnings=32
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_deprecated_max_segment_reclaim=10
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_destroy_on_exit=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_arena_reserve=1048576
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_arena_purge_mult=1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_deprecated_purge_extend_delay=1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_disallow_arena_alloc=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_retry_on_oom=400
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_visit_abandoned=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_guarded_min=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_guarded_max=1073741824
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_guarded_precise=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_guarded_sample_rate=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_guarded_sample_seed=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_page_reclaim_on_free=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_page_full_retain=2
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_page_max_candidates=4
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_max_vabits=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_pagemap_commit=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_page_commit_on_demand=0
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_page_max_reclaim=-1
{base_ts}.378Z,0.378367,622c,6,Info [DFLog::Mimalloc] mi_option_page_cross_thread_max_reclaim=32
{base_ts}.379Z,0.379365,622c,6,Info [FLog::AppMemUsageStatus] 923286370
{base_ts}.386Z,0.386350,622c,6 [FLog::Output] *******
** Validated reflection database in 5.987310 ms, found 6927 entries with the following skipped: 0 enums, 0 properties, 0 functions, 0 events, 0 callbacks, 0 classes.
*******

{base_ts}.386Z,0.386350,622c,6,Info [FLog::SessionL2ValidationHelper] onSessionChange sc_count 1 sh_count 0 cur_l2ts 0 elapsed_l2b 0
{base_ts}.386Z,0.386350,622c,6,Info [FLog::TombstoneCache] [FlagCache] Setting holdout experiment state to: false
{base_ts}.386Z,0.386350,622c,6,Warning [FLog::LocalStorageHandler] Not available on the current platform.
{base_ts}.386Z,0.386350,622c,6 [FLog::ClientRunInfo] RobloxGitHash: ff65cb9ffb48338446fc555da0383f01e0b938b5
{base_ts}.386Z,0.386350,622c,6 [FLog::ClientRunInfo] The base url is http://www.roblox.com/
{base_ts}.386Z,0.386350,622c,6 [FLog::ClientRunInfo] The channel is production
{base_ts}.386Z,0.386350,622c,6,Info [DFLog::SystemDialogHandler] Platform handler was instanced.
{base_ts}.386Z,0.386350,622c,6,Info [DFLog::AppPlatformQoSEmergency] AppPlatformQoSEmergencyHandler was instanced.
{base_ts}.387Z,0.387348,622c,6,Warning [FLog::RobloxStarter] GlobalInstance module completed
{base_ts}.387Z,0.387348,622c,6,Warning [FLog::RobloxStarter] All PreDM modules completed
{base_ts}.387Z,0.387348,622c,6,Warning [FLog::RobloxStarter] RobloxStarter destroyed
{base_ts}.497Z,0.497557,622c,6,Warning [DFLog::WebLoginController] [len 18] WebLogin auth result: {{"accountBlob":""}}
{base_ts}.500Z,0.500563,0cfc,6,Info [FLog::WndProcessCheck] waitForNewPlayerProcess new waiting for mutex result is 0X000000, ERROR The operation completed successfully.
{base_ts}.500Z,0.500563,0cfc,6,Info [FLog::WndProcessCheck] WindowsAppReloadTelemetry MutexWaitResultLoopUpdate
{base_ts}.508Z,0.508581,622c,6,Info [FLog::WindowsNativeAdsProtocol] Getting WindowsNativeAdsImpl instance
{base_ts}.508Z,0.508581,622c,6,Info [FLog::WindowsNativeAdsProtocol] Setting dependencies for WindowsNativeAdsImpl
{base_ts}.508Z,0.508581,622c,6,Warning [FLog::RobloxStarter] Starting module: DataModel
{base_ts}.508Z,0.508581,622c,6 [FLog::SingleSurfaceApp] initializeWithAppStarter
{base_ts}.508Z,0.508581,622c,6 [FLog::SingleSurfaceApp] initializeSingleton
{base_ts}.512Z,0.512590,622c,6 [FLog::Output] Evaluating deferred inferred crashes
{base_ts}.519Z,0.519606,622c,6 [FLog::SingleSurfaceApp] setStage: (stage:Native)
{base_ts}.519Z,0.519606,622c,6,Warning [FLog::SingleSurfaceApp] instantiate controllers
{base_ts}.519Z,0.519606,622c,6 [FLog::SurfaceController] SurfaceController[_:1]::SurfaceController, count: 1
{base_ts}.519Z,0.519606,622c,6,Warning [FLog::SingleSurfaceApp] instantiate experience coordinator
{base_ts}.520Z,0.520608,622c,6,Info [DFLog::CoordinatorMemoryPressure] Setting memory pressure threshold to none
{base_ts}.521Z,0.521610,622c,6,Warning [FLog::DataModelStarterModule] Launching experience at 2338325648
{base_ts}.521Z,0.521610,622c,6 [FLog::SingleSurfaceApp] launchUGCGame: (stage:Native).
{base_ts}.521Z,0.521610,622c,6 [FLog::SingleSurfaceApp] launchUGCGameInternal
{base_ts}.523Z,0.523615,622c,6 [FLog::SingleSurfaceApp] applyLocale
{base_ts}.524Z,0.524617,622c,6,Info [FLog::UgcExperienceController] UgcExperienceController: initialize:
{base_ts}.528Z,0.528626,622c,100 [FLog::DataModelPatchConfigurer] [_DataModelPatch] Load configs for user key: app:6253640119406273492
{base_ts}.528Z,0.528626,5bb0,6,Warning [FLog::Output] Hello world ...!
{base_ts}.528Z,0.528626,5bb0,6,Warning [FLog::Output] Hello ***187381***
{base_ts}.528Z,0.528626,5bb0,6,Warning [FLog::Output] Hello 198367!
{base_ts}.528Z,0.528626,5bb0,6,Warning [FLog::Output] Hello World! **CLI-199819**
{base_ts}.529Z,0.529628,622c,100 [FLog::DataModelPatchConfigurer] [_InExperiencePatch] Load configs for user key: app:6253640119406273492
{base_ts}.530Z,0.530631,622c,100 [FLog::DataModelPatchConfigurer] [_InExperiencePatch] Retrieving patch
{base_ts}.530Z,0.530631,622c,100,Warning [FLog::DataModelPatchConfigurer] getCachedPatch Local Patch DataModelPatchConfig : type=1, assetId=80471914653504, assetVersion=8356, maxAppVersion=723, localAssetURI=rbxasset://models/InExperience/InExperience.rbxm
{base_ts}.530Z,0.530631,622c,100,Warning [FLog::DataModelPatchConfigurer] getCachedPatch: config: 80471914653504.8356
{base_ts}.531Z,0.531633,622c,100,Warning [FLog::DataModelPatchConfigurer] getCachedPatch: ContentProvider does not have content
{base_ts}.531Z,0.531633,622c,100,Error [FLog::DataModelPatchConfigurer] getCachedPatch: failed to load core script patch!
{base_ts}.531Z,0.531633,622c,6 [LOGCHANNELS + 1] RBXCRASH: No verified patch could be loaded
{base_ts}.561Z,0.561700,5bb0,6,Info [FLog::Audio] OutputDevice 0: Speakers (THX Spatial) [Virtual] 48000/8
{base_ts}.571Z,0.571722,5bb0,6,Info [FLog::Audio] InputDevice 0: Microphone (Realtek USB Audio)([8377dba1-63be-4f31-b93c446c6bd9c001]) 48000/2/4
{base_ts}.571Z,0.571722,5bb0,6,Info [FLog::Audio] InputDevice 1: Microphone (3- Fifine Microphone)([c3a81497-50de-474b-b1ae827a7a0ab46f]) 192000/1/4
{base_ts}.748Z,0.748881,622c,6 [FLog::Output] Studio D3D9 GPU: NVIDIA GeForce RTX 4060 Ti
{base_ts}.748Z,0.748881,622c,6 [FLog::Output] Studio D3D9 GPU: Vendor 10de Device 2803
{base_ts}.748Z,0.748881,622c,6 [FLog::Output] Studio D3D9 Driver: nvldumdx.dll 32.0.15.9649
{base_ts}.757Z,0.757893,622c,6 [FLog::Output] ESGamePerfMonitor GPU: Vendor 000010de Device 00002803
{base_ts}.757Z,0.757893,622c,6 [FLog::Output] ESGamePerfMonitor GPU: SubSys 13dd196e Revision 000000a1
{base_ts}.757Z,0.757893,622c,6 [FLog::Output] ESGamePerfMonitor GPU: DedicatedVidMem 8335130624 DedicatedSysMem 0 SharedSystemMemory 8516429824
{base_ts}.897Z,5.897697,6768,6,Error [FLog::HangMonitor] Timeout while checking if the monitor step should be skipped

'@

function New-RandomHex {
    param([int]$Length = 5)
    $chars = '0123456789ABCDEF'
    $result = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $Length; $i++) {
        [void]$result.Append($chars[(Get-Random -Minimum 0 -Maximum $chars.Length)])
    }
    $result.ToString()
}

function Get-LogsDirectory {
    $localAppData = $env:LOCALAPPDATA
    if ([string]::IsNullOrWhiteSpace($localAppData)) {
        $localAppData = [Environment]::GetFolderPath('UserProfile')
    }
    Join-Path (Join-Path $localAppData 'Roblox') 'logs'
}

function Remove-ExistingLogs {
    param([string]$Directory)
    if (Test-Path -LiteralPath $Directory) {
        Get-ChildItem -LiteralPath $Directory -Filter '*_Player_*_last.log' -File -ErrorAction SilentlyContinue | Remove-Item -Force
    }
}

function Resize-Bytes {
    param(
        [byte[]]$Bytes,
        [int]$TargetSizeBytes
    )
    if ($Bytes.Length -eq $TargetSizeBytes) {
        return $Bytes
    }
    $output = New-Object byte[] $TargetSizeBytes
    [Array]::Copy($Bytes, $output, [Math]::Min($Bytes.Length, $TargetSizeBytes))
    return $output
}

function New-LogFile {
    param(
        [string]$Directory,
        [string]$ClientVersion,
        [string]$SessionId,
        [datetime]$BaseDateTime,
        [string]$RobloxUserId,
        [int]$TargetSizeBytes
    )
    $utcDate = $BaseDateTime.ToUniversalTime()
    $fileTs = $utcDate.ToString('yyyyMMddTHHmmssZ', $InvariantCulture)
    $baseTs = $utcDate.ToString('yyyy-MM-ddTHH:mm:ss', $InvariantCulture)
    $filename = "$ClientVersion`_$fileTs`_Player`_$SessionId`_last.log"
    $filepath = Join-Path $Directory $filename
    $content = $LogTemplate.Replace('{base_ts}', $baseTs).Replace('{file_ts}', $fileTs).Replace('{session}', $SessionId).Replace('{user_id}', $RobloxUserId).Replace('{{', '{').Replace('}}', '}')
    $bytes = $Utf8.GetBytes($content)
    if ($bytes.Length -lt $TargetSizeBytes) {
        $paddingText = "`n$baseTs.999Z,0.999999,0000,0 [FLog::Output] PADDING: " + ('X' * (($TargetSizeBytes - $bytes.Length) + 128))
        $paddingBytes = $Utf8.GetBytes($paddingText)
        $combined = New-Object byte[] ($bytes.Length + $paddingBytes.Length)
        [Buffer]::BlockCopy($bytes, 0, $combined, 0, $bytes.Length)
        [Buffer]::BlockCopy($paddingBytes, 0, $combined, $bytes.Length, $paddingBytes.Length)
        $bytes = $combined
    }
    $bytes = Resize-Bytes -Bytes $bytes -TargetSizeBytes $TargetSizeBytes
    [System.IO.File]::WriteAllBytes($filepath, $bytes)
    [System.IO.File]::SetCreationTimeUtc($filepath, $utcDate)
    [System.IO.File]::SetLastWriteTimeUtc($filepath, $utcDate)
    [System.IO.File]::SetLastAccessTimeUtc($filepath, $utcDate)
}

if ($null -eq $LogsDir) {
    $LogsDir = Get-LogsDirectory
}

New-Item -ItemType Directory -Path $LogsDir -Force | Out-Null

if (-not $NoDelete) {
    Remove-ExistingLogs -Directory $LogsDir
}

$minBytes = $MinKB * 1024
$maxBytes = $MaxKB * 1024

if ($minBytes -le 0 -or $maxBytes -le 0 -or $minBytes -gt $maxBytes) {
    throw 'Invalid size range. Ensure 0 < MinKB <= MaxKB.'
}

$now = [datetime]::UtcNow

for ($i = 0; $i -lt $Count; $i++) {
    $randomSeconds = Get-Random -Minimum 0 -Maximum (($Days * 24 * 3600) + 1)
    $baseDate = $now.AddSeconds(-$randomSeconds)
    $sessionId = New-RandomHex 5
    $targetSize = Get-Random -Minimum $minBytes -Maximum ($maxBytes + 1)
    New-LogFile -Directory $LogsDir -ClientVersion $Version -SessionId $sessionId -BaseDateTime $baseDate -RobloxUserId $UserId -TargetSizeBytes $targetSize
}

Write-Host "Generated $Count log file(s) in '$LogsDir'."

