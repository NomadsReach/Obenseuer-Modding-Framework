#!/bin/sh
# Fails when a public Core type shares a name with a game type (global types beat `using ObenseuerCore;`).
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TYPES="${1:-/c/Users/Ricky/source/ObenseuerReverseEng/Game/generated/25969064/types-Assembly-CSharp.txt}"
bad=0
for n in $(grep -h -o -E "public (static |sealed |abstract )*(partial )?(class|enum|struct|interface|delegate [A-Za-z<>]+) [A-Za-z_]+" "$ROOT"/src/*.cs "$ROOT"/src/*/*.cs "$ROOT"/webui/src/*.cs | awk '{print $NF}' | sort -u); do
  if grep -q -E "^[A-Za-z]+ $n$" "$TYPES"; then echo "COLLIDES: $n"; bad=1; fi
done
[ $bad -eq 0 ] && echo "names ok"
exit $bad
