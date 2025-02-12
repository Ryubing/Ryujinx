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

# Check if user already has a manual Avalonia scaling override or session type is x11
if [[ -n "${AVALONIA_GLOBAL_SCALE_FACTOR-}" || "$(echo "$XDG_SESSION_TYPE")" == "x11" ]]; then
    echo "Scaling: Performed by environment, skipping." >&2
else
    # Attempt to get desktop scaling from environment (GNOME)
    if [[ "$(echo "$XDG_CURRENT_DESKTOP")" == "GNOME" ]] && command -v gsettings >/dev/null; then
        echo -n 'Scaling: GNOME desktop scaling query...' >&2
        SCALING="$(gsettings get org.gnome.desktop.interface scaling-factor)"
        SCALING="${SCALING##* }"
        echo "found! Factor: ${SCALING}" >&2

    # Attempt to get desktop scaling from X Query (Others)
    elif command -v xrdb >/dev/null && command -v bc >/dev/null; then
        echo -n 'Scaling: X FreeType DPI scaling query...' >&2
        dpi="$(xrdb -get Xft.dpi)"
        if [[ -n "${dpi}" ]]; then
            SCALING=$(echo "scale=2; ${dpi}/96" | bc)
            echo "found! Factor: ${SCALING}" >&2
        fi
    fi

    if [[ -z "${SCALING-}" || "${SCALING-}" == "0" ]]; then
        echo 'Scaling: Unset scaling value, using default scaling.' >&2
        SCALING="1"
    fi

    COMMAND="$COMMAND AVALONIA_GLOBAL_SCALE_FACTOR=$SCALING"
fi

exec $COMMAND "$SCRIPT_DIR/$RYUJINX_BIN" "$@"