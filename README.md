# KinaUnaAzure


KinaUna is an app for managing family data, and the KinaUnaAzure solution contains the backend APIs, frontend web app, and IdentityServer.


The APIs and IdentityServer are also intended to be used by mobile apps.
A mobile app exists, I am working on making it open source too, but I'm not entirely sure if it is ready for that yet.


KinaUna was initially a simple ASP.Net MVC solution for learning about ASP.Net, MVC, JavaScript, and Visual Studio, but it quickly turned into an app I and my family used regularly.


So, it has slowly and steadily evolved with new features and adding more and more technologies, principles, patterns.

It's been mostly a vehicle for learning and for my own family's use, so many patterns, practices, and principles are only superficially implemented. I.e. I wanted to learn about microservices, so I separated the api's from the frontend app into their own servers. 
Initially I added two servers, but to keep costs down I merged them back into one single api server. In principle, the api server could and should be separated into many more servers.
This also means that I have done very little in terms of optimization.

It is designed for deployment to Azure Web Apps, with SQL Server database for persisting data and Azure Blobs for images. SignalR is used for realtime updates.
Generally I have tried to use DI to avoid tight coupling with these depencies, so it should be possible to use alternate solutions without too much work, but I haven't explicitly designed it for portability.


### The fundamental requirements for me:
- Users should be in control of all access to their data.
- All access to data needs to verify that the current user should be allowed to access it. Static files, such as pictures should also have restricted access.
- Privacy is very important, personal data should never be visible to anyone not explicitly authorized to access it. For example, email addresses should not appear in urls and logs.
- Multilingual, it needs to support viewing content in multiple languages.

### Initial features:
- Add/remove family member (currently it is add/remove child, but you can add any person)
- Access management (control who has access to your content)
- Timeline (view all content in chronological order)
- Notes (use notes for anything that doesn't fit in the other types of content)
- Calendar
- Pictures (picture gallery, picture information, tags, comments)
- Videos (video gallery, video information, tags, comments)
- Sleep (collect sleep data)
- Skills (record when skills are acquired)
- Vocabulary
- Measurements (track height and weight)
- Contacts
- Friends
- Locations (places lived, visited, or just of interest)
- Vaccinations
- Profile management
- For KinaUna administrators: 
    - Manage translations
    - Manage page texts (about page, terms and conditions, privacy, etc.)
    - Manage supported languages


### Currently wanted/missing features:
- Landing page
- Help and support
- Search
- Reminders


### Potential future features:
- Todo
- Kanban
- Messaging
- Documents
- Copy items to another person
- For KinaUna administrators: 
    - Manage users
    - Send messages to users
    - Analytics tools (currently done with Application Insights)

