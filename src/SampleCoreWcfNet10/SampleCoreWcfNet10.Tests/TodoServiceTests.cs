using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using AwesomeAssertions;

namespace SampleCoreWcfNet10.Tests;

public sealed class TodoServiceTests(TodoServiceFixture fixture) : IClassFixture<TodoServiceFixture>
{
    [Fact]
    public void Partner_can_run_full_todo_lifecycle()
    {
        using var factory = fixture.CreateChannelFactory(fixture.Certs.Partner);
        var client = factory.CreateChannel();

        var added = client.Add("  write integration tests ");
        added.Id.Should().NotBeEmpty();
        added.Title.Should().Be("write integration tests");
        added.IsCompleted.Should().BeFalse();
        added.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        client.GetAll().Should().ContainSingle(t => t.Id == added.Id);

        var completed = client.Complete(added.Id);
        completed.IsCompleted.Should().BeTrue();
        client.Get(added.Id)!.IsCompleted.Should().BeTrue();

        client.Delete(added.Id).Should().BeTrue();
        client.Get(added.Id).Should().BeNull();
        client.Delete(added.Id).Should().BeFalse();
    }

    [Fact]
    public void Completing_unknown_todo_returns_typed_fault()
    {
        using var factory = fixture.CreateChannelFactory(fixture.Certs.Partner);
        var client = factory.CreateChannel();
        var id = Guid.NewGuid();

        var act = () => client.Complete(id);

        act.Should().Throw<FaultException<TodoNotFoundFaultDto>>().Which.Detail.Id.Should().Be(id);
    }

    [Fact]
    public void Blank_title_is_rejected()
    {
        using var factory = fixture.CreateChannelFactory(fixture.Certs.Partner);
        var client = factory.CreateChannel();

        var act = () => client.Add("   ");

        act.Should().Throw<FaultException>().WithMessage("Title is required.");
    }

    [Fact]
    public void Without_client_certificate_the_tls_handshake_fails()
    {
        using var factory = fixture.CreateChannelFactory(clientCertificate: null);
        var client = factory.CreateChannel();

        var act = () => client.GetAll();

        act.Should().Throw<CommunicationException>();
    }

    [Theory]
    [MemberData(nameof(RejectedCertificates))]
    public void Certificate_not_on_allow_list_is_rejected(string _, Func<TestCertificates, X509Certificate2> pick)
    {
        using var factory = fixture.CreateChannelFactory(pick(fixture.Certs));
        var client = factory.CreateChannel();

        var act = () => client.GetAll();

        // 401 from the certificate handler surfaces as MessageSecurityException on the WCF client.
        act.Should().Throw<MessageSecurityException>();
    }

    public static TheoryData<string, Func<TestCertificates, X509Certificate2>> RejectedCertificates => new()
    {
        { "issued by our CA but not allow-listed", c => c.UnknownPartner },
        { "issued by a foreign CA", c => c.Rogue },
    };
}
