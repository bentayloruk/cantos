Strainer is the heart of the filters system.

* static Filters dictionary contains all of the filters.
* Two ways to add filter Types:
	* Static GlobalFilter method
	* Extend method which is called by Context.AddFilters (and internally to add the global filters to an instance).

It would be nice to extend the filter system so you could provide named delegates.  This would allow for some flexible inline filter creation code.  Funnily enough, this is what I want to do :)


