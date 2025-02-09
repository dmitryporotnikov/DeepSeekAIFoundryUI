# DeepSeekAIFoundryUI

This is a simple WPF application written in C#. It acts as an AI chatbot by sending your questions to a configured AI model. It can store chat history in a local database for later reference.

To run the app, open the solution in Visual Studio or Visual Studio Code, restore NuGet packages, and build the project. If you have an `appsettings.json` file, make sure it has valid values for ModelKey, ModelEndpoint, and ModelName.

When the app starts, you can type questions into the text box and press Send. The chat history is saved locally, and you can create new sessions or clear history. If you need to change settings, click the Settings button.

If you want to keep secrets private, do not commit `appsettings.json` to the repository. Instead, create your own copy of it locally.

Feel free to adjust this project to suit your needs. You can modify the UI, switch to a different AI model, or change how chats are stored. 
