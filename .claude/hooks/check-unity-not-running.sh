#!/usr/bin/env bash
# PreToolUse hook (Bash matcher). Blocks Unity batchmode invocations while a Unity
# Editor process is already running for this project - Unity refuses a second
# instance on the same project, so batchmode hangs/fails with:
#   "Aborting batchmode due to fatal error: another Unity instance is running
#    with this project open."
# Deliberately avoids jq (not guaranteed installed) - just greps the raw stdin
# JSON payload for the command text, which is safe since a legitimate Bash
# command containing "Unity.exe" is the only realistic source of that substring.

input="$(cat)"

if echo "$input" | grep -qi "Unity\.exe"; then
  running=""
  if command -v powershell.exe >/dev/null 2>&1; then
    running=$(powershell.exe -NoProfile -Command "(Get-Process Unity -ErrorAction SilentlyContinue).Count" 2>/dev/null | tr -d '\r\n')
  elif command -v pgrep >/dev/null 2>&1; then
    running=$(pgrep -x Unity 2>/dev/null | wc -l | tr -d ' ')
  fi

  if [ -n "$running" ] && [ "$running" != "0" ]; then
    printf '%s' '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"A Unity Editor process is already running for this project. Unity refuses a second instance on the same project, so batchmode will hang or fail with: Aborting batchmode due to fatal error: another Unity instance is running with this project open. Close the Unity Editor window first, then retry."}}'
    exit 0
  fi
fi

exit 0
