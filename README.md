🌟 Introduction 

In both our personal and professional lives, we all write down notes and create "to-do" lists. Whether it's setting New Year's goals, 
managing finances, planning a summer vacation, or remembering important life events, we rely on different methods to keep track of what needs to be done.\
Usually we use notepads or apps for these reminders.

However, we often forget simple things like: exercising regularly, reading more books or calling our grandparents more often. \
These tasks are easy to put off with the thought, "I'll do it tomorrow." \
This problem of delaying things hurts our productivity.

I've found that planning my day or week in advance improves my productivity. But manually writing to-do lists repeatedly can become annoying, 
and eventually I stop doing it. I needed a solution to be more productive and disciplined without the constant effort of manual planning,
a tool that acts like a personal assistant, consistently reminding me of my obligations.

To solve this problem, I made an app to help organize daily activities better.

🗓️ TaskMinder

**TaskMinder** is a web application designed to simplify your daily planning. 
It consists of single page with four main features:

1. **7-Days Schedule**: A weekly overview of your committed tasks, making sure you stay on track with your goals.
2. **One-Time Tasks**: A section for non-repeating tasks that need to be completed once, such as:
    - organize a bachelor party
    - research best investing options for retirement
3. **Repeating Tasks**: A section for recurring tasks such as:
    - exercise every other day
    - read at least 30 minutes each day
    - work at least 45 minutes each day on personal project outside work
    - listen at least 30 minutes of podcast every other day
4. **Notepads**: Create as many notepads as needed to store general information, such as
    - budgets
    - vacation packing list
    - wish list for future purchase

Each task can have an optional due date. When a due date is within the next 7 days, the task is automatically added to your schedule. 
Repeating tasks can be set to recreate automatically after a specified interval, such as reminding you to register your car annually.
This greatly reduces friction in helping us complete what we plan to do.

## Architecture

The solution is split into clear backend and frontend layers:

- `MyFeatures` contains the ASP.NET Core API, controllers, DTOs, validation, middleware, and startup composition.
- `Core` contains the application and domain logic, including `TaskTemplateService` and the task recurrence and ordering rules.
- `Infrastructure` contains EF Core entities, `MyFeaturesDbContext`, repositories, and migrations.
- `MyFeaturesUI` contains the Angular frontend and generated API client.

## Stack

- .NET 8 / ASP.NET Core Web API
- EF Core 8
- Angular 17
- FluentValidation
- Mapster
- Serilog
- xUnit + Moq + EF Core InMemory for backend tests

## What This Project Shows

- Clear separation between API, service/domain, and persistence concerns.
- A domain model that distinguishes task templates from concrete task occurrences.
- Automated backend tests for critical task flows such as scheduling, recurrence, and validation.
- Structured error handling and logging for operational visibility.

## Running The Project

- Build backend: `dotnet build .\MyFeatures.sln`
- Run backend tests: `dotnet test Core.Tests\Core.Tests.csproj`
- Build frontend: `cd MyFeaturesUI && npm run build`

---

> "Changes that seem small and unimportant at first will compound into remarkable results if you're willing to stick with them for years. We all deal with setbacks but in the long run, the quality of our lives often depends on the quality of our habits."  
> — *James Clear*


