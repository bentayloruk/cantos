Cantos is a static site generator.




---
FrontMatter
---
Body

Input to Cantos is a list of streams.



A stream that starts with --- will be considered to contain front matter.  A stream not starting with --- will be copied to an output stream without modification.  A stream that starts with --- but subsequently does not contain a closing --- (end of front matter) will be considered invalid and an error will be thrown.

Cantos does not hold all streams in memory.  Instead, Cantos plugins operate on StreamInfo records that contain stream meta-data, identity and the ability to create a reader for the stream.  This should mean that Cantos can operate on very large sites (we are trading speed for better memory profile).

All Cantos out-of-the-box features will be built using extension methods accessible to all.  This should mean that the application if more flexible than having two models.


Stages

Generator
Converter
Tags

* StreamOuput generation.  Output generators.
* StreamOutput generation.  Post output generators.  For things like categories and pages.
* StreamOutput enhancement.  Meta tags etc (e.g. table of contents)
     * TOC modify path of toc outputs.  Add meta data to global template context.

## TOC Plugin

* Give it a root path.
* Yield streamoutput not below root path.
* For others...
* Alter paths according to NumberWang.
* Build TOC from front matter.
* Return list with modifications and the global front matter changes.

## Blog Plugin

* Give it a in path (e.g. _posts).
* Give it an out path generator.
* Genrate posts into stream.
* Tag somehow?  Plugins that index pages and their content?

* StreamConvertWrite.  Write via a list of convertors.

Blog generator.
* Generates streams to be converted.  Puts them at a new URI (depending on the scheme).  Tags them with some meta?
* Blog XML generator reads all of the blog tagged streams.


Old 
* StreamInput generation.  Input generators.
* StreamInput enhancers.  Maybe, modify meta-data.
