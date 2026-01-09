var builder = DistributedApplication.CreateBuilder(args);


builder.AddProject<Projects.BankMore_Account_Api>("bankmore-account-api");

builder.AddProject<Projects.BankMore_Transfer_Api>("bankmore-transfer-api");

builder.Build().Run();
