---
categories: [empty, tech, test]
layout: blog
title: A Title!
# Some sample yaml for test :)
a: 123                     # an integer
b: "123"                   # a string, disambiguated by quotes
c: 123.0                   # a float
d: !!float 123             # also a float via explicit data type prefixed by (''' !! ''')
e: !!str 123               # a string, disambiguated by explicit type
f: !!str Yes               # a string via explicit type
g: Yes                     # a boolean True
h: Yes we have No bananas  # a string, "Yes" and "No" disambiguated by context.
---
When using the _posts directory, Cantos supports Jekyll style post names.
{{page.a}}
{{page.b}}
Test
