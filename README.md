# **What is this?** #

This is an alternate difficulty calculator for [osu!](https://osu.ppy.sh/) maps. It is meant to work as a companion application for players that might want to see a (hopefully) more accurate representation of a map's difficulty, as the one currently in the game has some oversights (some discussion [here](https://www.reddit.com/r/osugame/comments/2gzf9d/most_over_and_underrated_maps_star_difficultywise/)).

### Screenshots ###

![text diffs](screenshots/text-diffs.jpg)

![graph diffs](screenshots/graph-diffs.jpg)

### Features ###

+ Calculates difficulty of an osu! mapset based on what map is selected in-game, in the background
+ Difficulty graphs (as well as a familiarly-scaled rating system)
    + currently, these graphs are difficulty (y) vs time (x)
+ Automatically hides itself when in-game (stops analyzing when hidden or minimized)
+ Logs all difficulty results to an easy-to-parse XML that may come in handy (and saves on calculations)
```xml
<root>
  <mapset title="Bios Epic Edition" artist="Hiroyuki Sawano feat. Mika Kobayashi" creator="xChippy">
    <map version="King" totalDiff="282.506503372079" jumpDiff="176.527853868931" streamDiff="48.2813679065478"
     burstDiff="36.0730535599447" coupletDiff="0" sliderDiff="21.624228036656" />
  </mapset>
  <mapset title="ChaiN De/structioN" artist="sakuzyo feat. Hatsune Miku" creator="Shiirn">
    <map version="Loneliness" totalDiff="265.876510272099" jumpDiff="118.104800312151" streamDiff="81.84861013557"
     burstDiff="46.6906321499775" coupletDiff="0" sliderDiff="19.2324676744006" />
  </mapset>
</root>
```

### Difficulty calculation considerations ###

Currently considered features: jumps, streams, triplets, bursts, sliders

Planned considerations: patterns, couplets, weird timing, top ranked plays (ie a map where all the top plays are nomod vs doubletime, along with accuracy and combo. I think this is all available in the [osu! api](https://github.com/ppy/osu-api/wiki) but I admit that I have not looked into it)

### Where can I get a copy? ###

Grab the latest binary distribution from [Downloads](https://bitbucket.org/countcutright/osu-diffcalc/downloads) or compile from source

### Reporting issues ###

+ Create new issue in `Issues` section
+ Provide a copy of any output logs (right now it's shown in the console) 
+ Provide a link to any maps that may cause issues
+ Describe what the issue is, what problems it may be causing, etc

### Development ###

+ Clone latest commit, develop, test, submit pull request

### How can I get my pull request accepted? ###

+ Provide reasoning
+ Demonstrate an improvement
    + for performance tweaks, show timing improvements (or provide reasoning)
    + for difficulty algorithm tweaks, give a few map examples of repo version rating versus your rating and some justification as to why this new rating is more appropriate

### Who do I talk to? ###

+ Repo owner (through github or [message me on osu!](https://osu.ppy.sh/u/mastaa_p), although I'm rarely online)
