# QuaMergeDriver

because for some reason, you want to use git to collab on a map

## How to use (assuming this spaghetti actually works lol):
Set up your git repository's config file:
```bash
git config merge.qua.name = "custom merge driver for .qua files" # or whatever, tbh idk if this matters or not
git config merge.qua.driver = "path/to/executable %O %A %B blockSize" # blockSize is a number of milliseconds used for grouping up objects
                                                                      # so that patterns are compared instead of individual notes
                                                                      # replace it with an integer
                                                                      # set it to like a measure length or something
```

Add the following line to your git repository's .gitattributes file:
`*.qua text merge=qua`

Happy merging! (it's probably a buggy mess)




oh yeah, don't use the code written as an example,
unless you're gonna use it as an example of bad code
