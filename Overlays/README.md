# Example Overlays

### TNH Score Log

> ![Preview Image](https://github.com/user-attachments/assets/2528f85c-0fa1-4e1d-9832-5d3e35604092)

**Direct URL: [tnh.overlays.bacur.xyz](https://tnh.overlays.bacur.xyz)**\
Download: [DownGit](https://downgit.github.io/#/home?url=https://github.com/TakingFire/H3Status/tree/main/Overlays/TNHScoreLog)

## Usage

### In OBS

1. Create a new Browser Source.
2. Add the overlay URL to the source.
   - If downloading, check `Local file` and select `index.html`.
3. Set the Width and Height. A **minimum** of 400x250 is recommended.
4. To restart the connection, enable `Refresh browser when scene becomes active`, then hide/show the source.

## Configuring

If you download the overlay, you may configure it directly from [`config.js`](TNHScoreLog/config.js). The changes will be applied when the overlay is refreshed.

If using the direct URL, you may add any value in [`config.js`](TNHScoreLog/config.js) to the URL. For example, to hide the Ammo Panel and make the Event Log longer, you can use the following URL:\
[`tnh.overlays.bacur.xyz?showAmmoPanel=false&eventLogLength=12`](https://tnh.overlays.bacur.xyz?showAmmoPanel=false&eventLogLength=12)
