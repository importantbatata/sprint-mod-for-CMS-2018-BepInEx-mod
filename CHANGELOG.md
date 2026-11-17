VERSION HISTORY
1.3.0 - Footstep playback follows active sprint speed; normalizes velocity for
        the game's existing footstep volume, clip selection and high-speed cutoff.
1.2.0 - Native typed speed field, 100-250%; settings above Continue; Continue last.
1.1.0 - Native pause-menu settings submenu replaces the generic Unity popup.
        Original menu buttons/title/selection restored on return; input safeguards.
1.0.2 - Prevents Escape affecting both settings and pause menu in the same frame;
        restores menu controls on disable and handles configuration save failures.
1.0.1 - Rebuilt against game runtime libraries; error handling for menu insertion.
1.0.0 - Initial build. KNOWN BROKEN: wrong runtime reference caused System.Action
        TypeLoadException when Escape opened the menu. Historical archive only.

All six compiled versions are retained in the all-versions bundle. Use one only.
Exact source is available for 1.0.0, 1.0.2 and 1.3.0. Intermediate source snapshots
for 1.0.1, 1.1.0 and 1.2.0 were not retained and are not represented as recovered.
