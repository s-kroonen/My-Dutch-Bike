#!/usr/bin/env bash
# PreToolUse hook (Bash matcher). Blocks any Bash command whose text contains a
# "Co-Authored-By: Claude" trailer - this repo has an explicit user preference
# (see CLAUDE.md) to never add that trailer to commits, overriding the default
# Claude Code commit workflow.

input="$(cat)"

if echo "$input" | grep -q "Co-Authored-By: Claude"; then
  printf '%s' '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"This repo has an explicit no-Claude-co-author rule for commits (see CLAUDE.md). Remove the Co-Authored-By: Claude trailer from the commit message and retry."}}'
  exit 0
fi

exit 0
