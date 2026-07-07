using PonPon.Modules.Payment.Tests;

var tests = new (string Name, Action Run)[]
{
    (nameof(OrderPaymentSecurityTests.ConvertsBahtToSatangUsingExplicitRounding),
        new OrderPaymentSecurityTests().ConvertsBahtToSatangUsingExplicitRounding),
    (nameof(OrderPaymentSecurityTests.RejectsReusingChargeFromDifferentPaymentMethod),
        new OrderPaymentSecurityTests().RejectsReusingChargeFromDifferentPaymentMethod)
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}
