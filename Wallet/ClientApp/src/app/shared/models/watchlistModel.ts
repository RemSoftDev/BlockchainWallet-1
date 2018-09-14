export class WatchlistModel {
  userEmail: string;
  address: string;
  isContract: boolean;
  notificationOptions: NotificationOptions;
  tokenDecimalPlaces: number;

  constructor(email: string, address: string, isContract: boolean, notificationOptions: NotificationOptions, tokenDecimalPlaces : number) {
    this.address = address;
    this.userEmail = email;
    this.isContract = isContract;
    this.notificationOptions = notificationOptions;
    this.tokenDecimalPlaces = tokenDecimalPlaces;
  }
}

export class NotificationOptions {
  isWithoutNotifications: boolean;
  whenTokenOrEtherIsSent: boolean;
  tokenOrEtherSentName: string = 'ETH';
  whenAnythingWasSent: boolean;

  whenNumberOfTokenOrEtherWasSent: boolean;
  numberOfTokenOrEtherThatWasSentFrom: number;
  numberOfTokenOrEtherThatWasSentTo: number;
  numberOfTokenOrEtherWasSentName: string = 'ETH';
  whenTokenOrEtherIsReceived: boolean;
  tokenOrEtherReceivedName: string = 'ETH';
  whenNumberOfTokenOrEtherWasReceived: boolean;
  numberOfTokenOrEtherWasReceived: number;
  tokenOrEtherWasReceivedName: string = 'ETH';

  whenNumberOfContractTokenWasSent: boolean;
  numberOfContractTokenWasSent: number;
  whenNumberOfContractWasReceivedByAddress: boolean;
  numberOfTokenWasReceivedByAddress: number;
  addressThatReceivedNumberOfToken: string;
}
