import type { Asset } from './asset';

export interface AiChatRequest {
  question: string;
}

export interface AiChatResponse {
  answer: string;
  rows: Asset[];
  totalCount: number;
}

export const AI_EXAMPLES: string[] = [
  'Show me all laptops currently assigned to the Presales department.',
  'Which assets are currently available?',
  'How many Dell laptops do we have?',
  'Which assets are currently assigned to Ahmed?'
];
