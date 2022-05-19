Select top 200 * from 
( 
(SELECT a.[DisplayID],b.Name ,[Server] , FullDatasource, b.EditorDisplay, cast (b.COG as nvarchar(max)) as COG,  '0' as Collection
FROM [PIVisionBeforeUtility].[dbo].[DisplayDatasources]a, [PIVisionBeforeUtility].[dbo].[View_Displays]b 
where a.DisplayID=b.DisplayID  and FullDatasource like '%|%' and EditorDisplay like '%"SymbolType":"value"%' and name like '%01_SITE_PAR_ecm%' )
union
(select DisplayID,Name,'' as Server, '' as FullDatasource ,EditorDisplay,cast (COG as nvarchar(max)) as COG, '1' as Collection 
from [PIVisionBeforeUtility].[dbo].[View_Displays] where  EditorDisplay like '%StencilSymbols%'
)
)
d
where Server='XFPISYS' 
order by DisplayID