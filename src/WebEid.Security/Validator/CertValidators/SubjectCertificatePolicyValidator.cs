namespace WebEid.Security.Validator.CertValidators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Exceptions;
    using Microsoft.Extensions.Logging;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Security;

    internal sealed class SubjectCertificatePolicyValidator : ISubjectCertificateValidator
    {
        private readonly ICollection<Oid> disallowedSubjectCertificatePolicies;
        private readonly ILogger logger;

        public SubjectCertificatePolicyValidator(ICollection<Oid> disallowedSubjectCertificatePolicies, ILogger logger)
        {
            this.disallowedSubjectCertificatePolicies = disallowedSubjectCertificatePolicies
                                                        ?? throw new ArgumentNullException(nameof(disallowedSubjectCertificatePolicies));
            this.logger = logger;
        }

        /// <summary>
        /// Validates that the user certificate policies match the configured policies.
        /// </summary>
        /// <param name="subjectCertificate">the user certificate.</param>
        /// <exception cref="UserCertificateDisallowedPolicyException">when user certificate policy does not match the configured policies.</exception>
        /// <exception cref="UserCertificateInvalidPolicyException">when user certificate policy is invalid.</exception>
        public Task Validate(X509Certificate2 subjectCertificate)
        {
            try
            {
                var cert = DotNetUtilities.FromX509Certificate(subjectCertificate);
                var extensionValue = cert?.GetExtensionValue(X509Extensions.CertificatePolicies);
                var certificatePolicies = CertificatePolicies.GetInstance(extensionValue?.GetOctets());
                if (certificatePolicies?.GetPolicyInformation()
                    .Any(policy =>
                        this.disallowedSubjectCertificatePolicies
                        .Any(disallowedPolicy =>
                            disallowedPolicy.Value == policy.PolicyIdentifier.Id)) ?? false)
                {
                    throw new UserCertificateDisallowedPolicyException();
                }
            }
            catch (Exception ex) when (!(ex is UserCertificateDisallowedPolicyException))
            {
                throw new UserCertificateParseException(ex);
            }
            this.logger?.LogDebug("User certificate does not contain disallowed policies.");

            return Task.CompletedTask;
        }
    }
}
