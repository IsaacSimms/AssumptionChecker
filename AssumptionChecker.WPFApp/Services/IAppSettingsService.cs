///// used to test that the AnalyzeAsync method correctly sends the specified model in the request body to the backend API. The test sets up a fake HTTP handler to intercept the request, captures the body,   /////
///// and verifies that the model sent matches the expected value. This ensures that the service correctly includes the model information when making API calls.                                                /////

namespace AssumptionChecker.WPFApp.Services
{
    public interface IAppSettingsService
    {
        Models.AppSettings Load();
        void Save(Models.AppSettings settings);
    }
}