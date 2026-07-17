namespace Adeeb.Modules.Vocabulary.Domain;

public enum VocabularyLevel { A1 = 0, A2 = 1, B1 = 2, B2 = 3, C1 = 4, C2 = 5 }
public enum VocabularyQuestionType { Translation = 0, FillBlank = 1, OddWordReplacement = 2, Synonym = 3, Antonym = 4, WordOrder = 5 }
public enum VocabularySessionMode { DailyPractice = 0, MistakeReview = 1, FreePractice = 2, Test = 3 }
public enum VocabularyContentStatus { Draft = 0, Published = 1, Archived = 2 }
public enum VocabularyRelationType { Synonym = 0, Antonym = 1 }
public enum VocabularySessionStatus { InProgress = 0, Completed = 1 }
