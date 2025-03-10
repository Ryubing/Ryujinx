#!/bin/sh

SCRIPT_DIR=$(dirname "$(realpath "$0")")

if [ -f "$SCRIPT_DIR/Ryujinx.Headless.SDL2" ]; then
    RYUJINX_BIN="Ryujinx.Headless.SDL2"
fi

if [ -f "$SCRIPT_DIR/Ryujinx" ]; then
    RYUJINX_BIN="Ryujinx"
fi

if [ -z "$RYUJINX_BIN" ]; then
    exit 1
fi

COMMAND="env LANG=C.UTF-8 DOTNET_EnableAlternateStackCheck=1"

if command -v gamemoderun > /dev/null 2>&1; then
    COMMAND="$COMMAND gamemoderun"
fi

# Check if user already has a manual Avalonia scaling override or session type is x11.
if [[ -n "${AVALONIA_GLOBAL_SCALE_FACTOR-}" || "$(echo "$XDG_SESSION_TYPE")" == "x11" ]]; then
    echo "Scaling: Performed by environment, skipping." >&2
else
    # Query monitor config directly (GNOME), default display only.
    if [[ "$(echo "$XDG_CURRENT_DESKTOP")" == "GNOME" && -f ~/.config/monitors.xml ]] then
        echo -n 'Scaling: Monitor config located, querying scale...' >&2
        SCALING="$(grep '<scale' ~/.config/monitors.xml -m 1 | cut -f2 -d">"|cut -f1 -d"<")"
        SCALING="${SCALING##* }"
        echo "found! Factor: ${SCALING}" >&2

    # Fallback to X DPI query for others.
    # Plasma handles this fine, GNOME will always round up e.g. 1.25 -> 2.00.
    elif command -v xrdb >/dev/null; then
        echo -n 'Scaling: Attempting to get scaling from X DPI value...' >&2
        dpi="$(xrdb -get Xft.dpi)"
        if [[ -n "${dpi}" ]]; then
            SCALING=$(echo "scale=2; ${dpi}/96" | bc)
        fi
        echo "found! Factor: ${SCALING}"

    # Query kscreen-doctor for Plasma as a fallback.
    elif [[ "$(echo "$XDG_CURRENT_DESKTOP")" == "KDE" ]] && command -v kscreen-doctor >/dev/null; then
        echo -n 'Scaling: Attempting to get Plasma desktop scaling factor...' >&2
        SCALING="$(kscreen-doctor --outputs | grep "Scale" -m 1)"
        SCALING="${SCALING##* }"
        SCALING=$(echo $SCALING | sed 's/\x1B\[[0-9;]*m//g') # Trim ANSI chars from ksd output.
        echo "found! Factor: ${SCALING}"
    fi

    if [[ -z "${SCALING-}" || "${SCALING-}" == "0" ]]; then
        echo 'Unset invalid scaling value' >&2
        SCALING="1"
    fi

    COMMAND="$COMMAND AVALONIA_GLOBAL_SCALE_FACTOR=$SCALING"
fi

exec $COMMAND "$SCRIPT_DIR/$RYUJINX_BIN" "$@"