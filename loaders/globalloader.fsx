#r "../_lib/Fornax.Core.dll"

type SiteInfo = {
    title: string
    description: string
}

let loader (projectRoot: string) (siteContent: SiteContents) =
    siteContent.Add({title = "m.atthew.uk"; description = "Recent Posts"})

    siteContent
