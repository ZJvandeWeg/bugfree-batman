bugfree-batman
==============

How to run it
-------------

To train the bot, we ran `tools/train.sh` about a thousand times. We used the following Ruby script to do so, but you can do so by hand or use the scripting language of your choosing.

```sh
cd tools
ruby -e "1000.times { |i| puts %Q{BEURT #{i}}; system('./train.sh') }"
```

To then run the bot, you can just run `tools/play_with_launch.sh`.