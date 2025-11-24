import { ClientNamePipe } from './client-name.pipe';

describe('ClientNamePipe', () => {
  it('create an instance', () => {
    const pipe = new ClientNamePipe();
    expect(pipe).toBeTruthy();
  });
});
