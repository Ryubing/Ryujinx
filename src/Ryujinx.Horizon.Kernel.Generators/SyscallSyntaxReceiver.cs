using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Horizon.Kernel.Generators
{
    class SyscallSyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> SvcImplementations { get; }

        public SyscallSyntaxReceiver()
        {
            SvcImplementations = [];
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is ClassDeclarationSyntax classDeclaration) || classDeclaration.AttributeLists.Count == 0)
            {
                return;
            }

            if (!classDeclaration.AttributeLists.Any(attributeList =>
                    attributeList.Attributes.Any(x => x.Name.GetText().ToString() == "SvcImpl")))
            {
                return;
            }

            foreach (MemberDeclarationSyntax memberDeclaration in classDeclaration.Members)
            {
                if (memberDeclaration is MethodDeclarationSyntax methodDeclaration)
                {
                    VisitMethod(methodDeclaration);
                }
            }
        }

        private void VisitMethod(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.AttributeLists.Count == 0)
            {
                return;
            }

            if (methodDeclaration.AttributeLists.Any(attributeList =>
                    attributeList.Attributes.Any(x => x.Name.GetText().ToString() == "Svc")))
            {
                SvcImplementations.Add(methodDeclaration);
            }
        }
    }
}
