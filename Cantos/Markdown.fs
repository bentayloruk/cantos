module Markdown

open MarkdownDeep

///Converts markdown to html.
let mdToHtml markdown = 
    let md = Markdown()
    md.ExtraMode <- false
    md.SafeMode <- false
    md.Transform(markdown)


