# **About** #

This is an alternate difficulty calculator for [osu!](https://osu.ppy.sh/) maps. It is meant to work as a companion application for players that might want to see a (hopefully) more accurate representation of a map's difficulty, as the one currently in the game has some oversights (some discussion [here](https://www.reddit.com/r/osugame/comments/2gzf9d/most_over_and_underrated_maps_star_difficultywise/).

### Features ###

+ Calculates difficulty of an osu! mapset based on what map is selected in-game, in the background
+ Automatically hides itself when in-game (stops analyzing when hidden or minimized)
+ **ADD SCREENSHOTS**
+ Difficulty graphs
+ **ADD SCREENSHOTS**
+ Logs all difficulty results to an easy-to-parse XML that may come in handy (and saves on calculations)
+ **example xml**

### Difficulty calculation considerations ###

Currently considered features: jumps, streams, triplets, bursts, sliders
Planned considerations: patterns, couplets, weird timing, top ranked plays (ie a map where all the top plays are nomod vs doubletime, along with accuracy and combo. I think this is all available in the [osu! api](https://github.com/ppy/osu-api/wiki) but I admit that I have not looked into it)

### Testing this diffcalc ###

+ Grab the latest binary distribution from [Downloads](https://bitbucket.org/countcutright/osu-diffcalc/downloads)
+ Compile from source

### Reporting issues ###

+ Create new issue in [Issues](https://bitbucket.org/countcutright/osu-diffcalc/issues) section
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

+ Repo owner (through bitbucket or [message me on osu!](https://osu.ppy.sh/u/mastaa_p))
+ Admins, if there are ever any