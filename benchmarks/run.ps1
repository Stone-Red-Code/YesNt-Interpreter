$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$benchmarks = @(
  @{ Name='arith_loop'; YesNt='arith_loop.ynt'; Py='arith_loop.py'; Js='arith_loop.js' },
  @{ Name='func_call_loop'; YesNt='func_call_loop.ynt'; Py='func_call_loop.py'; Js='func_call_loop.js' },
  @{ Name='list_ops'; YesNt='list_ops.ynt'; Py='list_ops.py'; Js='list_ops.js' }
)

$commands = @(
  @{ Lang='YesNt'; Build={ param($b) "dotnet ../YesNt.Interpreter.App/bin/Release/net10.0/yesnt.dll $($b.YesNt)" } },
  @{ Lang='Python'; Build={ param($b) "python $($b.Py)" } },
  @{ Lang='Node'; Build={ param($b) "node $($b.Js)" } }
)

function Invoke-Timed([string]$command) {
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  cmd /c $command > $null
  $sw.Stop()

  if ($LASTEXITCODE -ne 0) {
    throw "Command failed with exit code ${LASTEXITCODE}: $command"
  }

  return [double]$sw.Elapsed.TotalMilliseconds
}

$iterations = 8
$results = @()

foreach ($benchmark in $benchmarks) {
  foreach ($command in $commands) {
    $cmd = & $command.Build $benchmark

    [void](Invoke-Timed $cmd) # warm-up run

    $times = @()
    for ($i = 0; $i -lt $iterations; $i++) {
      $times += Invoke-Timed $cmd
    }

    $sorted = $times | Sort-Object
    $mean = ($times | Measure-Object -Average).Average
    $median = if ($iterations % 2 -eq 0) {
      ($sorted[$iterations / 2 - 1] + $sorted[$iterations / 2]) / 2
    } else {
      $sorted[[int]($iterations / 2)]
    }

    $results += [pscustomobject]@{
      Benchmark = $benchmark.Name
      Language = $command.Lang
      MeanMs = [math]::Round($mean, 2)
      MedianMs = [math]::Round($median, 2)
      MinMs = [math]::Round($sorted[0], 2)
      MaxMs = [math]::Round($sorted[-1], 2)
    }
  }
}

$results = $results | Sort-Object Benchmark, Language
$results | Format-Table -AutoSize

Write-Host "`nRelative slowdown (lower baseline is better):"
$slowdowns = foreach ($name in ($results.Benchmark | Select-Object -Unique)) {
  $group = $results | Where-Object Benchmark -eq $name
  $yesnt = ($group | Where-Object Language -eq 'YesNt').MeanMs
  $python = ($group | Where-Object Language -eq 'Python').MeanMs
  $node = ($group | Where-Object Language -eq 'Node').MeanMs

  [pscustomobject]@{
    Benchmark = $name
    YesNt_vs_Python_x = [math]::Round($yesnt / $python, 1)
    YesNt_vs_Node_x = [math]::Round($yesnt / $node, 1)
  }
}

$slowdowns | Format-Table -AutoSize

Write-Host "`nCSV:"
$results | ConvertTo-Csv -NoTypeInformation
