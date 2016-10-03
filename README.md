# GenerateLogCfg

GenerateLogCfg is a port of the [Openport LogCfg Generator](https://github.com/hunterjm/openport-logcfg/)
by Jason Hunter for the Windows .Net platform.

GenerateLogCfg generates a `logcfg.txt` file which is used by the internal
[Openport logger](http://www.tactrix.com/), based on
[RomRaider](http://www.romraider.com/) logger profile and logging definitions.

## Usage

GenerateLogCfg is launched from the command-line.  The basic syntax is:

```
GenerateLogCfg DEFINITIONS PROFILE OUTPUT
```

So, for example

```
GenerateLogCfg logger_STD_EN_v123.xml profile.xml logcfg.txt
```

More detailed usage information can be obtained using `GenerateLogCfg --help`.

## License

BSD 3 clause