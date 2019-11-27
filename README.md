# C# Fuzzy Logic Api
This Api executes inferences by fuzzy logic concept on C# Plain Old CLR Object associating an [Expression](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression) object defined in native .NET Framework.

## 1) Before you begin: Abstraction
If do you want to delve into [Fuzzy Logic](https://plato.stanford.edu/entries/logic-fuzzy/) theory (such as mathematical theorems, postulates, and [Morgan's law](https://en.wikipedia.org/wiki/De_Morgan%27s_laws)) it's strongly recommended to look for other references to satisfy your curiosity and / or your research need. Through here this git post, you'll access only a practical example to execute the Fuzzy Logic in real world applications; then, the focus in this article is not diving on philosophical dialogue with only a pratical porpuse.

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
    public int IntegrityPercentage { get; set; }

    [XmlElement]
    public int TruthPercentage { get; set; }

    [XmlElement]
    public int JusticeSensePercentage { get; set; }
    
    [XmlElement]
    public int MistakesPercentage
    { 
    	get
	{
	    return ((100-IntegrityPercentage) + (100-TruthPercentage) + (100-JusticeSensePercentage))/3;
	}
    }
}

//Crisp Logic expression that represents Honesty Profiles:
static Expression<Func<HonestAssesment, bool>> _honestyProfile = (h) =>
(h.IntegrityPercentage > 75 && h.JusticeSensePercentage > 75 && h.TruthPercentage > 75) || //First group
(h.IntegrityPercentage > 90 && h.JusticeSensePercentage > 60 && h.TruthPercentage > 50) || //Second group
(h.IntegrityPercentage > 70 && h.JusticeSensePercentage > 90 && h.TruthPercentage > 80) || //Third group
(h.IntegrityPercentage > 65 && h.JusticeSensePercentage > 100 && h.TruthPercentage > 95); //Last group

```
The boolean expression broken is one capacity derived from [System.Linq.Expressions.Expression](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression) class, converting any block of code to representational string; the derived class who will auxiliate with this job is [BinaryExpression](https://docs.microsoft.com/pt-br/dotnet/api/system.linq.expressions.binaryexpression?view=netframework-4.7.2): the boolean expression will be sliced in binary tree of smaller boolean expression, whose rule will prioritize the slice where the conditional expression is contained 'OR', then sliced by 'AND' conditional expression.
```cs
//First group of assesment:
h.IntegrityPercentage > 75;
h.JusticeSensePercentage > 75;
h.TruthPercentage > 75;

//Second group of assesment:
h.IntegrityPercentage > 90;
h.JusticeSensePercentage > 60;
h.TruthPercentage > 50;

//Third group of assesment:
h.IntegrityPercentage > 70;
h.JusticeSensePercentage > 90;
h.TruthPercentage > 80;

//Last group of assesment:
h.IntegrityPercentage > 65;
h.JusticeSensePercentage > 100;
h.TruthPercentage > 95;
```
This functionality contained in the .NET Framework is the trump card to mitigate the appraisal value that the evaluated profiles have conquered or how close they have come to reach any of the 4 defined valuation groups, for example:
```cs
HonestAssesment profile1 = new HonestAssesment()
{
    IntegrityPercentage = 90,
    JusticeSensePercentage = 80,
    TruthPercentage = 70
};
string inference_p1 = FuzzyLogic<HonestAssesment>.GetInferenceResult(_expression, ResponseType.Json, profile1);
```
Look at "HitsPercentage" property. The inference on Profile 1, with 66% of Honest
```json
{
	"ID": 0,
	"HitsPercentage": "66%",
	"Data": {
		"IntegrityPercentage": 90,
		"TruthPercentage": 70,
		"JusticeSensePercentage": 80,
		"MistakesPercentage": 20
	},
	"PropertiesNeedToChange": [
		"IntegrityPercentage"
	],
	"RatingsReport": [
		false,
		true,
		true
	],
	"ErrorsQuantity": 1
}
```
```cs
HonestAssesment profile2 = new HonestAssesment()
{
    IntegrityPercentage = 50,
    JusticeSensePercentage = 63,
    TruthPercentage = 30
};
string inference_p2 = FuzzyLogic<HonestAssesment>.GetInferenceResult(_expression, ResponseType.Json, profile2);
```
The inference on Profile 2, with 33% of Honest, that is "Sometimes honest", like a tutorialspoint figure.
```json
{
	"ID": 0,
	"HitsPercentage": "33%",
	"Data": {
		"IntegrityPercentage": 50,
		"TruthPercentage": 30,
		"JusticeSensePercentage": 63,
		"MistakesPercentage": 52
	},
	"PropertiesNeedToChange": [
		"IntegrityPercentage",
		"TruthPercentage"
	],
	"RatingsReport": [
		false,
		true,
		false
	],
	"ErrorsQuantity": 2
}
```
```cs
HonestAssesment profile3 = new HonestAssesment()
{
    IntegrityPercentage = 46,
    JusticeSensePercentage = 48,
    TruthPercentage = 30
};
string inference_p3 = FuzzyLogic<HonestAssesment>.GetInferenceResult(_expression, ResponseType.Json, profile3);
```
The inference on Profile 3, with 0% of Honest, that is "Extremely dishonest", like a figure above.
```json
{
	"ID": 0,
	"HitsPercentage": "0%",
	"Data": {
		"IntegrityPercentage": 46,
		"TruthPercentage": 30,
		"JusticeSensePercentage": 48,
		"MistakesPercentage": 58
	},
	"PropertiesNeedToChange": [
		"IntegrityPercentage",
		"JusticeSensePercentage",
		"TruthPercentage"
	],
	"RatingsReport": [
		false,
		false,
		false
	],
	"ErrorsQuantity": 3
}
```
```cs
HonestAssesment profile4 = new HonestAssesment()
{
    IntegrityPercentage = 91,
    JusticeSensePercentage = 83,
    TruthPercentage = 81
};
string inference_p4 = FuzzyLogic<HonestAssesment>.GetInferenceResult(_expression, ResponseType.Json, profile4);
```
The inference on Profile 4, with 100% of Honest, that is "Extremely honest", like a figure assesment.
```json
{
	"ID": 0,
	"HitsPercentage": "100%",
	"Data": {
		"IntegrityPercentage": 91,
		"TruthPercentage": 81,
		"JusticeSensePercentage": 83,
		"MistakesPercentage": 15
	},
	"PropertiesNeedToChange": [],
	"RatingsReport": [
		true,
		true,
		true
	],
	"ErrorsQuantity": 0
}
```
### 3.2) Design Pattern
The 'Fuzzy Logic API' developed with [Singleton Design Pattern](https://en.wikipedia.org/wiki/Singleton_pattern), structured with one private constructor, where have two arguments parameter: one [Expression](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression) object and one POCO object (defined in Generic parameter); but the developer will get the inference result by one line of code

```cs
//Like a inference object...
Inference<ModelToInfere> inferObj = FuzzyLogic<ModelToInfere>.GetInferenceResult(_expressionArg, modelObj);

//... get as xml string...
string inferXml = FuzzyLogic<ModelToInfere>.GetInferenceResult(_expressionArg, ResponseType.Xml, modelObj);

//...or json string.
string inferJson = FuzzyLogic<ModelToInfere>.GetInferenceResult(_expressionArg, ResponseType.Json, modelObj);
```
### 3.3) Dependecies
To add Fuzzy Logic Api as an Assembly or like classes inner in your Visual Studio project, you'll need to install [System.Linq.Dynamic](https://archive.codeplex.com/?p=dynamiclinq) dll, that can be installed by [nuget](https://www.nuget.org/packages/System.Linq.Dynamic/) reference or execute command on Nuget Package Console (Install-Package System.Linq.Dynamic).

----------------------
## Prize

Voted Best Project of June/2019 by [Code Project](https://www.codeproject.com/Articles/5092762/Csharp-Fuzzy-Logic-API)

----------------------
## License

[View MIT license](https://github.com/antonio-leonardo/FuzzyLogicApi/blob/master/LICENSE)
