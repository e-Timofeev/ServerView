Use [ServersList]

UPDATE [PhyServers]
SET Status = 
      (SELECT * FROM OPENROWSET(BULK N'D:\Projects\C# Basic\ServerView\EcmServerCard\Resources\none.png', SINGLE_BLOB) AS image)

UPDATE [VirServers]
SET Status = 
      (SELECT * FROM OPENROWSET(BULK N'D:\Projects\C# Basic\ServerView\EcmServerCard\Resources\none.png', SINGLE_BLOB) AS image)
