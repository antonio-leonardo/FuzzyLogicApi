# C# Fuzzy Logic Api
This Api executes inferences by fuzzy logic concept on C# Plain Old CLR Object associating an [Expression](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression) object defined in native .NET Framework.

## 1) Before you begin: Abstraction
If do you want to delve into [Fuzzy Logic](https://plato.stanford.edu/entries/logic-fuzzy/) theory (such as mathematical theorems, postulates, and [Morgan's law](https://en.wikipedia.org/wiki/De_Morgan%27s_laws)) it's strongly recommended to look for other references to satisfy your curiosity and / or your research need. Through here this git post, you'll access only a practical example to execute the Fuzzy Logic in real world applications; then, the focus in this article is not diving on philosophical dialogue with only a pratical porpuse. The new version update of this API using iteractions with [Parallelism](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel) or [Yield](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/yield) in some portions of code to increase performance; another advance are bugs fixes such like: lack of properties in XML output, output of inference in decimal numbers (following the most diffuse logical theory), and calibration of output values for greater accuracy. Important note: All constructors stay the same, with no impact to the any developer project.

## 2) Fuzzy Logic Concepts
This figure from [tutorialspoint](https://www.tutorialspoint.com/fuzzy_logic/fuzzy_logic_introduction.htm) site resumes the real concept of Fuzzy Logic: Nothing is absolutily true or false (for Fuzzy Logic); between 0 and 1 you have a interval from these extremes, beyond the limits of the boolean logic.

![From: https://www.tutorialspoint.com/fuzzy_logic/fuzzy_logic_introduction.htm](https://www.tutorialspoint.com/fuzzy_logic/images/fuzzy_logic_introduction.jpg)

## 3) Using the API

### 3.1) Core flow
The core concept had the first requirement: Defuzzyfication. In other words, generate a Fuzzy Logic results by [Crisp Input](https://blog.oureducation.in/tag/crisp-logic/) expression builded on Fuzzy Logic Engine (view figure bellow, from [Wikepdia](https://es.wikipedia.org/wiki/Defuzzificaci%C3%B3n) reference):

![From: https://es.wikipedia.org/wiki/Defuzzificaci%C3%B3n](https://upload.wikimedia.org/wikipedia/commons/2/22/Fuzzy_logic.png)

The rule of Fuzzy Logic Engine is: break apart any complex boolean expression (crisp input) that resolves a logical boolean problem in minor boolean parts rule (about the theory used of this complex boolean expression, view articles like [Many-Valued Logic](https://plato.stanford.edu/entries/logic-manyvalued/) or /[Classical Logic](https://plato.stanford.edu/entries/logic-classical/)).

Based on illustrative example above, let's create a Model class that represents the Honest charater like integrity, truth and justice sense percentage assesment for all and a boolean expression object that identifies the Honesty Profile, considering the minimal percentage to be a honest person:

```cs
[Serializable, XmlRoot]
public class HonestAssesment
{
    [XmlElement]
    public double Integrity { get; set; }

    [XmlElement]
    public double Truth { get; set; }

    [XmlElement]
    public double JusticeSense { get; set; }

    [XmlElement]
    public double MistakesAVG
    {
        get
        {
            return (Integrity + Truth - JusticeSense) / 3;
        }
    }
}

//Crisp Logic expression that represents Honesty Profiles:
static Expression<Func<HonestAssesment, bool>> _honestyProfile = (h) =>
(h.IntegrityPercentage > 75 && h.JusticeSensePercentage > 75 && h.TruthPercentage > 75) || //First group
(h.IntegrityPercentage > 90 && h.JusticeSensePercentage > 60 && h.TruthPercentage > 50) || //Second group
(h.IntegrityPercentage > 70 && h.JusticeSensePercentage > 90 && h.TruthPercentage > 80) || //Third group
(h.IntegrityPercentage > 65 && h.JusticeSensePercentage == 100 && h.TruthPercentage > 95); //Last group
```

The boolean expression broken is one capacity derived from [System.Linq.Expressions.Expression](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression) class, converting any block of code to representational string; the derived class who will auxiliate with this job is [BinaryExpression](https://docs.microsoft.com/pt-br/dotnet/api/system.linq.expressions.binaryexpression?view=netframework-4.7.2): the boolean expression will be sliced in binary tree of smaller boolean expression, whose rule will prioritize the slice where the conditional expression is contained 'OR', then sliced by 'AND' conditional expression.
```cs
//First group of assesment:
h.IntegrityPercentage > 0.75;
h.JusticeSensePercentage > 0.75;
h.TruthPercentage > 0.75;

//Second group of assesment:
h.IntegrityPercentage > 0.9;
h.JusticeSensePercentage > 0.6;
h.TruthPercentage > 0.5;

//Third group of assesment:
h.IntegrityPercentage > 0.7;
h.JusticeSensePercentage > 0.9;
h.TruthPercentage > 0.8;

//Last group of assesment:
h.IntegrityPercentage > 0.65;
h.JusticeSensePercentage > 1;
h.TruthPercentage > 0.95;
```

This functionality contained in the .NET Framework is the trump card to mitigate the appraisal value that the evaluated profiles have conquered or how close they have come to reach any of the 4 defined valuation groups, for example:
```cs
HonestAssesment profile1 = new HonestAssesment()
{
    IntegrityPercentage = 90,
    JusticeSensePercentage = 80,
    TruthPercentage = 70
};
string inference_p1 = FuzzyLogic<HonestAssesment>.GetInference(_honestyProfile, ResponseType.Json, profile1);
```
Look at "HitsPercentage" and "InferenceResult" properties.
The inference on Profile 1, with 0.67 of Honest (1 is Extremely Honest).
-Result of "inference_p1" string variable (JSON):
```json
{
    "ID":"72da723b-b879-474c-b2cc-6a11c5965b25",
    "InferenceResult":"0.67",
    "Data":
        {
            "IntegrityPercentage":90,
            "TruthPercentage":70,
            "JusticeSensePercentage":80,
            "MistakesPercentage":20
        },
    "PropertiesNeedToChange":["IntegrityPercentage"],
    "ErrorsQuantity":1
}
```
```cs
HonestAssesment profile2 = new HonestAssesment()
{
    IntegrityPercentage = 50,
    JusticeSensePercentage = 63,
    TruthPercentage = 30
};
string inference_p2 = FuzzyLogic<HonestAssesment>.GetInference(_honestyProfile, ResponseType.Xml, profile2);
```
The inference on Profile 2, with 33% of Honest, that is "Sometimes honest", like a tutorialspoint figure.
--Result of "inference_p2" string variable (XML):
```xml
<?xml version="1.0" encoding="utf-8"?>
<InferenceOfHonestAssesment>
  <PropertiesNeedToChange>IntegrityPercentage</PropertiesNeedToChange>
  <PropertiesNeedToChange>TruthPercentage</PropertiesNeedToChange>
  <ErrorsQuantity>2</ErrorsQuantity>
  <ID>efb7249e-568f-44d4-a8b3-ce728a243273</ID>
  <InferenceResult>0.33</InferenceResult>
  <Data>
    <IntegrityPercentage>50</IntegrityPercentage>
    <TruthPercentage>30</TruthPercentage>
    <JusticeSensePercentage>63</JusticeSensePercentage>
  </Data>
</InferenceOfHonestAssesment>
```
```cs
HonestAssesment profile3 = new HonestAssesment()
{
    IntegrityPercentage = 46,
    JusticeSensePercentage = 48,
    TruthPercentage = 30
};
var inference_p3 = FuzzyLogic<HonestAssesment>.GetInference(_honestyProfile, profile3);
```
The inference on Profile 3, with 0% of Honest, that is "Extremely dishonest", like a figure above.
--Result of "inference_p3" Api Model variable (like "Inference<HonestAssesment" object) in imagem bellow:

![From: https://github.com/antonio-leonardo/FuzzyLogicApi](https://raw.githubusercontent.com/antonio-leonardo/FuzzyLogicApi/master/FuzzyProfile3.PNG)

```cs
HonestAssesment profile4 = new HonestAssesment()
{
    IntegrityPercentage = 91,
    JusticeSensePercentage = 83,
    TruthPercentage = 81
};
List<HonestAssesment> allProfiles = new List<HonestAssesment>();
allProfiles.Add(profile1);
allProfiles.Add(profile2);
allProfiles.Add(profile3);
allProfiles.Add(profile4);
string inferenceAllProfiles = FuzzyLogic<HonestAssesment>.GetInference(_honestyProfile, ResponseType.Xml, allProfiles);
```
Inferences with all Profiles, in XML:
```xml
<?xml version="1.0" encoding="utf-8"?>
<InferenceResultOfHonestAssesment>
  <Inferences>
    <PropertiesNeedToChange>IntegrityPercentage</PropertiesNeedToChange>
    <ErrorsQuantity>1</ErrorsQuantity>
    <ID>8d79084c-9402-4683-833d-437cad86ef4a</ID>
    <InferenceResult>0.67</InferenceResult>
    <Data>
      <IntegrityPercentage>90</IntegrityPercentage>
      <TruthPercentage>70</TruthPercentage>
      <JusticeSensePercentage>80</JusticeSensePercentage>
    </Data>
  </Inferences>
  <Inferences>
    <PropertiesNeedToChange>IntegrityPercentage</PropertiesNeedToChange>
    <PropertiesNeedToChange>TruthPercentage</PropertiesNeedToChange>
    <ErrorsQuantity>2</ErrorsQuantity>
    <ID>979d4ebe-6210-46f4-a492-00df88591d17</ID>
    <InferenceResult>0.33</InferenceResult>
    <Data>
      <IntegrityPercentage>50</IntegrityPercentage>
      <TruthPercentage>30</TruthPercentage>
      <JusticeSensePercentage>63</JusticeSensePercentage>
    </Data>
  </Inferences>
  <Inferences>
    <PropertiesNeedToChange>IntegrityPercentage</PropertiesNeedToChange>
    <PropertiesNeedToChange>JusticeSensePercentage</PropertiesNeedToChange>
    <PropertiesNeedToChange>TruthPercentage</PropertiesNeedToChange>
    <ErrorsQuantity>3</ErrorsQuantity>
    <ID>9004cb7a-b75d-4452-9d9a-c8836a5531eb</ID>
    <InferenceResult>0</InferenceResult>
    <Data>
      <IntegrityPercentage>46</IntegrityPercentage>
      <TruthPercentage>30</TruthPercentage>
      <JusticeSensePercentage>48</JusticeSensePercentage>
    </Data>
  </Inferences>
  <Inferences>
    <ErrorsQuantity>0</ErrorsQuantity>
    <ID>36545dae-1dde-4bfd-a528-ae42a7a0748f</ID>
    <InferenceResult>1.00</InferenceResult>
    <Data>
      <IntegrityPercentage>91</IntegrityPercentage>
      <TruthPercentage>81</TruthPercentage>
      <JusticeSensePercentage>83</JusticeSensePercentage>
    </Data>
  </Inferences>
</InferenceResultOfHonestAssesment>
```
### 3.2) Design Pattern
The 'Fuzzy Logic API' developed with [Singleton Design Pattern](https://en.wikipedia.org/wiki/Singleton_pattern), structured with one private constructor, where have two arguments parameter: one [Expression](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression) object and one POCO object (defined in Generic parameter); but the developer will get the inference result by one line of code

```cs
//Like a inference object...
Inference<ModelToInfere> inferObj = FuzzyLogic<ModelToInfere>.GetInference(_honestyProfileArg, modelObj);

//... get as xml string...
string inferXml = FuzzyLogic<ModelToInfere>.GetInference(_honestyProfileArg, ResponseType.Xml, modelObj);

//...or json string.
string inferJson = FuzzyLogic<ModelToInfere>.GetInference(_honestyProfileArg, ResponseType.Json, modelObj);
```
### 3.3) Dependecies
To add Fuzzy Logic Api as an Assembly or like classes inner in your Visual Studio project, you'll need to install [System.Linq.Dynamic](https://archive.codeplex.com/?p=dynamiclinq) dll, that can be installed by [nuget](https://www.nuget.org/packages/System.Linq.Dynamic/) reference or execute command on Nuget Package Console (Install-Package System.Linq.Dynamic).

----------------------
## Award

Voted the Best Article of June/2019 by [Code Project](https://www.codeproject.com/Competitions/1082/Best-Article-of-June-2019.aspx#winners)

----------------------
## License

[View MIT license](https://github.com/antonio-leonardo/FuzzyLogicApi/blob/master/LICENSE)
