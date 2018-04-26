# Starbound Mod Downloader

An over-simplistic and very error prone mod downloader for PlayStarbound and GitHub.  
I made this to automatically download mods for the [Wardrobe-Addons](https://github.com/Silverfeelin/Starbound-Wardrobe-Addons) repository, but if you can find a reason to use this feel free to give it a go.

Don't expect any support or updates from me. Do feel free to use this as reference material on how (not) to build a better downloader.

## PlayStarbound

```
dotnet StarboundModDownloader.dll -i https://community.playstarbound.com/resources/some-resource.0000/ -o someResource.pak --overwrite -s
```

### Arguments

* `-i`, `--input`  
Resource URL.  
```
-i https://community.playstarbound.com/resources/frackinuniverse.2920/
```

* `-o`, `--output`  
Output file location.  
```
-o C:\Downloads\FrackinUniverse.zip
```

* `--overwrite`  
Allows overwriting of the output file if it already exists. Has no value.

* `-s`, `--session`  
The flag `-s` should be set to your `xf2_session` cookie, found in your browser. Without the cookie the tool won't be able to access the download link.  
If anyone knows how to create a session from the `xf2_user` cookie, feel free to let me know.

* `-v` `--previousversion`  
If set, aborts the download if the download version matches this number. The tool logs the version number, but you can also find it by looking at the URL of the `Download Now` button.  
```
-v 23922
```

#### Finding your `xf2_session` cookie

The below steps to find your cookie are probably similar in other browsers. Make sure you're logged in, otherwise the session cookie will still not work! If you close the browser the cookie will expire.

![Show Cookies](https://i.imgur.com/BV1sh2S.png "Show Cookie")

![xf2_session](https://i.imgur.com/2FE1e7z.png)

Copy the code after `Content`; this is your `xf2_session` cookie.

## GitHub

### Arguments

* `-i`, `--input`  
Repository URL.
```
-i https://github.com/Silverfeelin/Starbound-Wardrobe
```

* `-o`, `--output`  
Output file location.
```
-o C:\Downloads\Wardrobe.zip
```

* `--overwrite`  
Allows overwriting of the output file if it already exists. Has no value.

* `-s`, `--source`  
If set, downloads the source code (zip) instead of an uploaded asset. Has no value.

* `-p`, `--pattern`  
If `-s` is not set, this Regex pattern is used to download the first asset matching the pattern.  
```
-p Wardrobe\.pak
-p .*\.pak
```

* `-v` `--previousversion`  
If set, aborts the download if the release tag matches this version. The tool logs the tag of the release, but you can also find it by looking at the Release page of the repository. Keep in mind that releases marked as a `Pre-release` are ignored.
```
-v v1.3.2
```

## Error codes

The tool can exit with a couple of different error codes, to indicate what went wrong.  
Before exiting, the tool will log the specifics of the error.

| Code | Description |
|---|---|
| 0 | Everything went fine. |
| 1 | Invalid argument(s). You might be missing some required arguments, or the arguments have an incorrect value (for example a bad regex pattern). |
| 2 | Version matches `-v` argument. This can be used to prevent downloading the same release multiple times. |
| 3 | Download failed. Arguments seemed correct but something went wrong while downloading file file. The most probable cause for this is an aborted connection. |
| 4 | Saving file failed. Could be due to file/folder permissions. |
