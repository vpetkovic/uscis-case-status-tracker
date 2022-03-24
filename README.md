# USCIS Case Status Tracker

I ended up in a situation where I have to keep a close look at statuses of my GC applications, specifically I-765 as without EAD card I am not able to work. That means checking several times per day or hourly or even more frequent than that. 

USCIS is famous for their lengthy processing time and potential of requiring more information/documents if they werent provided enough. Acting fast was critical requirement for me to avoid potential extra delays as there are some other steps my employer needs to take before I can return to work.

USCIS had improved over the years ways someone can lookup case statuses:
- Unauthenticated way allows only one case per lookup 
- Authenticated way allows to store all cases for lookup but required 2FA login and sessions were very short

Both ways for frequent checks quickly become unintiuitive and annoying due to repetitive task of copy/pasting case number(s), or logging in with 2FA every hour 

<hr>

I helped myself (and now others) by creating 3 different types of bots. One of which is for on demand case lookup locally on PC while 2 others are meant to be deployed to Azure Functions (completely free). Here's the overview of each. 

- **CLI** [Local PC]: on demand case status lookup/tracking if you just want something to use out of the box. Features:
  - Multi Case lookup 
  - Case Tracking

- **HTTP Trigger** [Azure Function]: on demand case status tracking via HTTP Trigger. this type provides unique https URL to be called. This is useful if you want to setup any sort of automated workflow on the cloud yourself, such as `IFTTT`, `IOS Shortcuts`, `PowerApps`, `UIPath`, etc. Features:
    - Multi Case lookup
    - Case Tracking

- **Timer Trigger** [Azure Function]: This is one goes further than HTTP trigger as it reoccurs case status tracking via Time Trigger. It executes every minute. This bot also has few features that HTTP trigger doesn't have due to fact this is type of process that is unattended. Features:
    - Multi Case lookup
    - Case Tracking
    - Email Notification
    - Text/SMS Notification

*NOTE: Azure Functions are my personal preference but it can easily be replaced by AWS Lambdas. Feel free to contribute with such project types (bots)*

*NOTE: Azure Functions can incure some costs but they are trivial and it is in pennies range, mainly for storage usage if you decide to use case tracking instead of lookup. First 1_000_000 function invocations in a month are free. To put in prospective 1 month = 43830 minutes*

*NOTE: SendGrid can incure some costs. Free plan is limited to 100 per day. Unless you have more than 100 case status updates per day this should be free. Alternatively SMTP could be implemented (may turn it into a task)*
<hr>


### Features

- **Multi Case Lookup**: You can lookup up infinite number of receipt #. Depending how many cases you want to lookup in each run it may be considered as a Denial of Service attack to USCIS system. If you have large number of cases to lookup, which is unlikely for personal use, and still want to use this tool, you might consider batching cases per run, extending time between each run and/or using proxy. **You have been warned! Use this tool at your own risk!**


- **Case Tracking**: this feature allows case tracking instead of just case status lookup which means that each time bot checks for case status it will be able to determine if status has changed by comparing the results of the current run with stored information about the case from previous run. To use this you need to setup Azure Storage (explained below) to be able to persist results.


- **Email Notification**: this feature provides ability to send email to defined list of recepients. To avoid spamming it will **only** be triggered if there are changes to the case status. To use this you need to setup Sendgrid (explained below).


- **Text/SMS Notification**: this feature provides ability to send text/SMS to defined list of phone numbers. To avoid spamming it will **only** be triggered if there are changes to the case status. To use this you need to setup Twilio SMS (explained below) or use clever Email-To-SMS (explained below).

<hr>

### Azure Cloud Storage Setup
(comming soon)
### SendGrid Setup
(comming soon)
### Twilio SMS Setup
(comming soon)
### Email-to-SMS Setup
(comming soon)