namespace MetricsReporter.Tests.Processing.Parsers;

using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Processing.Parsers;

[TestFixture]
[Category("Unit")]
public sealed class RoslynMetricsDocumentWalkerTests
{

  [Test]
  public void Parse_WithCompleteDocument_BuildsHierarchyAndMetrics()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Metrics>
                  <Metric Name="MaintainabilityIndex" Value="85" />
                </Metrics>
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Metrics>
                      <Metric Name="MaintainabilityIndex" Value="82" />
                    </Metrics>
                    <Types>
                      <Type Name="LoaderApp" File="LoaderApp.cs" Line="12">
                        <Metrics>
                          <Metric Name="MaintainabilityIndex" Value="80" />
                        </Metrics>
                        <Members>
                          <Member Name="void LoaderApp.Initialize()" File="LoaderApp.cs" Line="20">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="78" />
                            </Metrics>
                          </Member>
                        </Members>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.SolutionName.Should().Be("TestSolution");

    var assembly = result.Elements.Single(e => e.Kind == CodeElementKind.Assembly);
    assembly.FullyQualifiedName.Should().Be("Rca.Loader");
    assembly.Metrics.Should().ContainKey(MetricIdentifier.RoslynMaintainabilityIndex);

    var ns = result.Elements.Single(e => e.Kind == CodeElementKind.Namespace);
    ns.Name.Should().Be("Rca.Loader");
    ns.ParentFullyQualifiedName.Should().Be("Rca.Loader");
    ns.ContainingAssemblyName.Should().Be("Rca.Loader");

    var type = result.Elements.Single(e => e.Kind == CodeElementKind.Type);
    type.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp");
    type.Source.Should().NotBeNull();
    type.Source!.Path.Should().Be("LoaderApp.cs");
    type.Source.StartLine.Should().Be(12);
    type.ContainingAssemblyName.Should().Be("Rca.Loader");

    var member = result.Elements.Single(e => e.Kind == CodeElementKind.Member);
    member.FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp.Initialize(...)");
    member.Source.Should().NotBeNull();
    member.Source!.StartLine.Should().Be(20);
    member.ContainingAssemblyName.Should().Be("Rca.Loader");
  }

  [Test]
  public void Parse_WithNoTargets_ReturnsEmptyElements()
  {
    // Arrange
    var document = XDocument.Parse("<CodeMetricsReport />");

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.SolutionName.Should().BeEmpty();
    result.Elements.Should().BeEmpty();
  }

  [Test]
  public void Parse_WithTargetButNoAssembly_ReturnsEmptyElements()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution" />
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.SolutionName.Should().Be("TestSolution");
    result.Elements.Should().BeEmpty();
  }

  [Test]
  public void Parse_WithAssemblyButNoNamespaces_ReturnsOnlyAssembly()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Metrics>
                  <Metric Name="MaintainabilityIndex" Value="85" />
                </Metrics>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.Elements.Should().HaveCount(1);
    result.Elements.Single().Kind.Should().Be(CodeElementKind.Assembly);
    result.Elements.Single().FullyQualifiedName.Should().Be("Rca.Loader");
  }

  [Test]
  public void Parse_WithNamespaceButNoTypes_ReturnsAssemblyAndNamespace()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Metrics>
                      <Metric Name="MaintainabilityIndex" Value="82" />
                    </Metrics>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.Elements.Should().HaveCount(2);
    result.Elements.Should().Contain(e => e.Kind == CodeElementKind.Assembly);
    result.Elements.Should().Contain(e => e.Kind == CodeElementKind.Namespace);
    result.Elements.Single(e => e.Kind == CodeElementKind.Namespace).Name.Should().Be("Rca.Loader");
  }

  [Test]
  public void Parse_WithTypeButNoMembers_ReturnsAssemblyNamespaceAndType()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Types>
                      <Type Name="LoaderApp" File="LoaderApp.cs" Line="12">
                        <Metrics>
                          <Metric Name="MaintainabilityIndex" Value="80" />
                        </Metrics>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.Elements.Should().HaveCount(3);
    result.Elements.Should().Contain(e => e.Kind == CodeElementKind.Assembly);
    result.Elements.Should().Contain(e => e.Kind == CodeElementKind.Namespace);
    result.Elements.Should().Contain(e => e.Kind == CodeElementKind.Type);
    result.Elements.Single(e => e.Kind == CodeElementKind.Type).FullyQualifiedName.Should().Be("Rca.Loader.LoaderApp");
  }

  [Test]
  public void Parse_WithGlobalNamespace_HandlesEmptyNamespaceName()
  {
    // Arrange - When Name attribute is missing, code uses "<global>", but when it's empty string, it uses ""
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace>
                    <Types>
                      <Type Name="GlobalClass" File="GlobalClass.cs" Line="10">
                        <Members>
                          <Member Name="void Method()" File="GlobalClass.cs" Line="20" />
                        </Members>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var ns = result.Elements.Single(e => e.Kind == CodeElementKind.Namespace);
    ns.Name.Should().Be("<global>", "missing Name attribute should default to <global>");
    var type = result.Elements.Single(e => e.Kind == CodeElementKind.Type);
    type.FullyQualifiedName.Should().Be("GlobalClass");
    var member = result.Elements.Single(e => e.Kind == CodeElementKind.Member);
    member.FullyQualifiedName.Should().Be("GlobalClass.Method(...)");
  }

  [Test]
  public void Parse_WithEmptyStringNamespaceName_HandlesAsEmpty()
  {
    // Arrange - When Name attribute is explicitly empty string, code uses it as-is
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="">
                    <Types>
                      <Type Name="GlobalClass" File="GlobalClass.cs" Line="10" />
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var ns = result.Elements.Single(e => e.Kind == CodeElementKind.Namespace);
    ns.Name.Should().BeEmpty("explicitly empty Name attribute is used as-is");
    var type = result.Elements.Single(e => e.Kind == CodeElementKind.Type);
    type.FullyQualifiedName.Should().Be("GlobalClass", "empty namespace name should still allow type FQN construction");
  }

  [Test]
  public void Parse_WithMissingAssemblyName_UsesDefaultName()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly>
                <Metrics>
                  <Metric Name="MaintainabilityIndex" Value="85" />
                </Metrics>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var assembly = result.Elements.Single(e => e.Kind == CodeElementKind.Assembly);
    assembly.FullyQualifiedName.Should().Be("<unknown-assembly>");
  }

  [Test]
  public void Parse_WithMissingTypeName_UsesDefaultName()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Types>
                      <Type File="Unknown.cs" Line="10">
                        <Metrics>
                          <Metric Name="MaintainabilityIndex" Value="80" />
                        </Metrics>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var type = result.Elements.Single(e => e.Kind == CodeElementKind.Type);
    type.Name.Should().Be("<unknown-type>");
    type.FullyQualifiedName.Should().Be("Rca.Loader.<unknown-type>");
  }

  [Test]
  public void Parse_WithMissingMemberName_HandlesGracefully()
  {
    // Arrange - When Name attribute is missing, code uses "<unknown-member>", but ExtractDisplayMethodName
    // processes it and may return empty string since "<unknown-member>" doesn't match method signature format
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Types>
                      <Type Name="LoaderApp" File="LoaderApp.cs" Line="12">
                        <Members>
                          <Member File="LoaderApp.cs" Line="20">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="78" />
                            </Metrics>
                          </Member>
                        </Members>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var member = result.Elements.Single(e => e.Kind == CodeElementKind.Member);
    // When Name is missing, code defaults to "<unknown-member>", but ExtractDisplayMethodName
    // processes it and may return empty string since it doesn't match method signature format
    member.Name.Should().NotBeNull("member name should not be null even if processing results in empty string");
    // The member should still be created and have a valid FQN (even if normalized)
    member.FullyQualifiedName.Should().NotBeNull("member FQN should be set even with missing name");
  }

  [Test]
  public void Parse_WithEmptyMemberName_HandlesGracefully()
  {
    // Arrange - When Name attribute is explicitly empty, ExtractDisplayMethodName may return empty string
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Types>
                      <Type Name="LoaderApp" File="LoaderApp.cs" Line="12">
                        <Members>
                          <Member Name="" File="LoaderApp.cs" Line="20" />
                        </Members>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var member = result.Elements.Single(e => e.Kind == CodeElementKind.Member);
    // Empty Name attribute results in empty string after processing, which is acceptable
    member.Name.Should().NotBeNull("member name should not be null even if empty");
  }

  [Test]
  public void Parse_WithMissingFileAttribute_ReturnsNullSource()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Types>
                      <Type Name="LoaderApp" Line="12">
                        <Members>
                          <Member Name="void Method()" Line="20" />
                        </Members>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var type = result.Elements.Single(e => e.Kind == CodeElementKind.Type);
    type.Source.Should().BeNull();
    var member = result.Elements.Single(e => e.Kind == CodeElementKind.Member);
    member.Source.Should().BeNull();
  }

  [Test]
  public void Parse_WithInvalidLineNumber_IgnoresLineNumber()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Types>
                      <Type Name="LoaderApp" File="LoaderApp.cs" Line="invalid">
                        <Members>
                          <Member Name="void Method()" File="LoaderApp.cs" Line="not-a-number" />
                        </Members>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var type = result.Elements.Single(e => e.Kind == CodeElementKind.Type);
    type.Source.Should().NotBeNull();
    type.Source!.Path.Should().Be("LoaderApp.cs");
    type.Source.StartLine.Should().BeNull();
    var member = result.Elements.Single(e => e.Kind == CodeElementKind.Member);
    member.Source.Should().NotBeNull();
    member.Source!.Path.Should().Be("LoaderApp.cs");
    member.Source.StartLine.Should().BeNull();
  }

  [Test]
  public void Parse_WithMissingMetrics_ReturnsEmptyMetrics()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Rca.Loader">
                    <Types>
                      <Type Name="LoaderApp" File="LoaderApp.cs" Line="12">
                        <Members>
                          <Member Name="void Method()" File="LoaderApp.cs" Line="20" />
                        </Members>
                      </Type>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    var assembly = result.Elements.Single(e => e.Kind == CodeElementKind.Assembly);
    assembly.Metrics.Should().BeEmpty();
    var ns = result.Elements.Single(e => e.Kind == CodeElementKind.Namespace);
    ns.Metrics.Should().BeEmpty();
    var type = result.Elements.Single(e => e.Kind == CodeElementKind.Type);
    type.Metrics.Should().BeEmpty();
    var member = result.Elements.Single(e => e.Kind == CodeElementKind.Member);
    member.Metrics.Should().BeEmpty();
  }

  [Test]
  public void Parse_WithMultipleTargets_UsesLastName()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="FirstSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0" />
            </Target>
            <Target Name="SecondSolution">
              <Assembly Name="Rca.Network, Version=1.0.0.0" />
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.SolutionName.Should().Be("SecondSolution");
    result.Elements.Should().HaveCount(2);
  }

  [Test]
  public void Parse_WithTargetWithoutName_PreservesPreviousSolutionName()
  {
    // Arrange
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Rca.Loader, Version=1.0.0.0" />
            </Target>
            <Target>
              <Assembly Name="Rca.Network, Version=1.0.0.0" />
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);

    // Assert
    result.SolutionName.Should().Be("TestSolution");
  }

  [Test]
  public void Parse_WithVariousMemberKinds_SetsMemberKind()
  {
    // Arrange: include Method, Property, Field, Event with names similar to Roslyn output.
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Sample.Assembly, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Sample">
                    <Types>
                      <NamedType Name="Sample.MyType">
                        <Members>
                          <Method Name="void MyType.DoWork()" File="MyType.cs" Line="10">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="90" />
                            </Metrics>
                          </Method>
                          <Property Name="int MyType.Count" File="MyType.cs" Line="20">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="85" />
                            </Metrics>
                          </Property>
                          <Field Name="int MyType._value" File="MyType.cs" Line="30">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="80" />
                            </Metrics>
                          </Field>
                          <Event Name="System.EventHandler MyType.OnChanged" File="MyType.cs" Line="40">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="75" />
                            </Metrics>
                          </Event>
                        </Members>
                      </NamedType>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);
    var members = result.Elements.Where(e => e.Kind == CodeElementKind.Member).ToList();

    // Assert
    members.Should().HaveCount(4);
    members.Single(m => m.Name == "DoWork").MemberKind.Should().Be(MemberKind.Method);
    members.Single(m => m.Name == "Count").MemberKind.Should().Be(MemberKind.Property);
    members.Single(m => m.Name == "_value").MemberKind.Should().Be(MemberKind.Field);
    members.Single(m => m.Name == "OnChanged").MemberKind.Should().Be(MemberKind.Event);
  }

  [Test]
  public void Parse_SkipsPropertyAccessors()
  {
    // Arrange: a property plus its accessor method; accessor should be skipped.
    var document = XDocument.Parse(
        """
        <CodeMetricsReport>
          <Targets>
            <Target Name="TestSolution">
              <Assembly Name="Sample.Assembly, Version=1.0.0.0">
                <Namespaces>
                  <Namespace Name="Sample">
                    <Types>
                      <NamedType Name="Sample.MyType">
                        <Members>
                          <Property Name="int Sample.MyType.Count" File="MyType.cs" Line="20">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="85" />
                            </Metrics>
                          </Property>
                          <Method Name="int Sample.MyType.get_Count()" File="MyType.cs" Line="21">
                            <Metrics>
                              <Metric Name="MaintainabilityIndex" Value="84" />
                            </Metrics>
                          </Method>
                        </Members>
                      </NamedType>
                    </Types>
                  </Namespace>
                </Namespaces>
              </Assembly>
            </Target>
          </Targets>
        </CodeMetricsReport>
        """);

    // Act
    var result = RoslynMetricsDocumentWalker.Parse(document);
    var members = result.Elements.Where(e => e.Kind == CodeElementKind.Member).ToList();

    // Assert
    members.Should().HaveCount(1, "property accessor should not be treated as a separate member");
    members[0].Name.Should().Be("Count");
    members[0].MemberKind.Should().Be(MemberKind.Property);
  }
}


